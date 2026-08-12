using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Claims;
using TuitionCenter.Models;
using TuitionCenter.Models.ViewModels;

namespace TuitionCenter.Controllers
{
    [Authorize]
    public class TeacherController : Controller
    {
        private readonly TuitionCenterDbContext _context;

        public TeacherController(TuitionCenterDbContext context)
        {
            _context = context;
        }

        private (int TeacherId, User? Teacher) GetCurrentTeacher()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int teacherId))
            {
                var fallbackTeacher = _context.Users.FirstOrDefault(u => u.Role == "Teacher");
                return (fallbackTeacher?.UserId ?? 0, fallbackTeacher);
            }

            var teacher = _context.Users.FirstOrDefault(u => u.UserId == teacherId);
            if (teacher == null)
            {
                teacher = _context.Users.FirstOrDefault(u => u.Role == "Teacher");
                if (teacher != null) teacherId = teacher.UserId;
            }
            return (teacherId, teacher);
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var (teacherId, teacher) = GetCurrentTeacher();
            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var teacherBatches = _context.Batches
                .Include(b => b.EnrollmentSubjects)
                .Where(b => b.TeacherId == teacherId)
                .ToList();

            int totalStudents = teacherBatches.Sum(b => b.EnrollmentSubjects.Count);
            int activeClasses = teacherBatches.Count(b => b.IsActive);

            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcomingSessions = _context.ClassSessions
                .Include(cs => cs.Batch).ThenInclude(b => b.Subject)
                .Include(cs => cs.Batch).ThenInclude(b => b.Class)
                .Include(cs => cs.Batch).ThenInclude(b => b.EnrollmentSubjects)
                .Where(cs => cs.TeacherId == teacherId && cs.SessionDate >= today)
                .OrderBy(cs => cs.SessionDate)
                .ThenBy(cs => cs.StartTime)
                .Take(6)
                .ToList();

            var nextSession = upcomingSessions.FirstOrDefault();
            var otherUpcoming = upcomingSessions.Skip(1).ToList();

            // Dynamic attendance calculation for this teacher
            var teacherSessionIds = _context.ClassSessions
                .Where(cs => cs.TeacherId == teacherId)
                .Select(cs => cs.SessionId)
                .ToList();

            double averageAttendance = 0;
            if (teacherSessionIds.Any())
            {
                var totalAttendanceRecords = _context.Attendances.Count(a => teacherSessionIds.Contains(a.SessionId));
                if (totalAttendanceRecords > 0)
                {
                    var presentCount = _context.Attendances.Count(a => teacherSessionIds.Contains(a.SessionId) && a.IsPresent);
                    averageAttendance = Math.Round((double)presentCount / totalAttendanceRecords * 100, 1);
                }
            }

            var viewModel = new TeacherDashboardViewModel
            {
                TeacherName = teacher.FullName,
                TeacherEmail = teacher.Email,
                TotalStudents = totalStudents,
                ActiveClasses = activeClasses,
                AverageAttendance = averageAttendance,
                CurrentAcademicYear = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",

                NextClass = nextSession != null ? new UpcomingClassViewModel
                {
                    SessionId = nextSession.SessionId,
                    Title = nextSession.Title,
                    BatchName = nextSession.Batch?.BatchName ?? "Batch",
                    SubjectName = nextSession.Batch?.Subject?.SubjectName ?? "Subject",
                    ClassName = nextSession.Batch?.Class?.ClassName ?? "Class",
                    StartTime = nextSession.StartTime,
                    EndTime = nextSession.EndTime,
                    EnrolledStudents = nextSession.Batch?.EnrollmentSubjects.Count ?? 0,
                    MeetingLink = string.IsNullOrEmpty(nextSession.MeetingLink) ? "https://meet.google.com/new" : nextSession.MeetingLink
                } : null,

                UpcomingClasses = otherUpcoming.Select(s => new UpcomingClassViewModel
                {
                    SessionId = s.SessionId,
                    Title = s.Title,
                    BatchName = s.Batch?.BatchName ?? "Batch",
                    SubjectName = s.Batch?.Subject?.SubjectName ?? "Subject",
                    ClassName = s.Batch?.Class?.ClassName ?? "Class",
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    EnrolledStudents = s.Batch?.EnrollmentSubjects.Count ?? 0,
                    MeetingLink = string.IsNullOrEmpty(s.MeetingLink) ? "https://meet.google.com/new" : s.MeetingLink
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Schedule(DateTime? startDate)
        {
            var (teacherId, teacher) = GetCurrentTeacher();
            if (teacher == null) return RedirectToAction("Login", "Account");

            DateTime start = startDate ?? DateTime.Today;
            int diff = (7 + (start.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime startOfWeek = start.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            var sessions = _context.ClassSessions
                .Include(cs => cs.Batch).ThenInclude(b => b.Subject)
                .Include(cs => cs.Batch).ThenInclude(b => b.Class)
                .Where(cs => cs.TeacherId == teacherId
                        && cs.SessionDate >= DateOnly.FromDateTime(startOfWeek)
                        && cs.SessionDate <= DateOnly.FromDateTime(endOfWeek))
                .ToList();

            var viewModel = new TeacherScheduleViewModel
            {
                TeacherName = teacher.FullName,
                TeacherEmail = teacher.Email,
                CurrentWeekStart = startOfWeek,
                CurrentWeekEnd = endOfWeek,
                WeekDateRangeDisplay = $"{startOfWeek:MMM d} - {endOfWeek:MMM d, yyyy}",
                Days = new List<DayScheduleViewModel>()
            };

            string[] colorThemes = { "bg-blue-600 text-white", "bg-emerald-600 text-white", "bg-indigo-600 text-white", "bg-purple-600 text-white" };

            for (int i = 0; i < 7; i++)
            {
                DateTime currentDate = startOfWeek.AddDays(i);
                var daySessions = sessions
                    .Where(s => s.SessionDate == DateOnly.FromDateTime(currentDate))
                    .OrderBy(s => s.StartTime)
                    .Select((s, index) => new ClassSessionViewModel
                    {
                        SessionId = s.SessionId,
                        Title = s.Title,
                        SubjectName = s.Batch?.Subject?.SubjectName ?? "Subject",
                        ClassName = s.Batch?.Class?.ClassName ?? "Class",
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        ColorTheme = colorThemes[Math.Abs(s.BatchId) % colorThemes.Length]
                    }).ToList();

                viewModel.Days.Add(new DayScheduleViewModel
                {
                    Date = currentDate,
                    DayOfWeek = currentDate.ToString("ddd").ToUpper(),
                    DayOfMonth = currentDate.Day,
                    IsToday = currentDate.Date == DateTime.Today,
                    Sessions = daySessions
                });
            }

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult ClassManagement()
        {
            var (teacherId, teacher) = GetCurrentTeacher();

            var dbBatches = _context.Batches
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .Include(b => b.TimeSlot)
                .Include(b => b.EnrollmentSubjects)
                .Where(b => b.TeacherId == teacherId)
                .ToList();

            var batchList = new List<TeacherBatchItemViewModel>();

            foreach (var b in dbBatches)
            {
                string classNameLower = (b.Class?.ClassName ?? "").ToLower();
                string level = "secondary";
                string badge = "SECONDARY";
                if (classNameLower.Contains("12") || classNameLower.Contains("higher")) { level = "class12"; badge = "HIGHER SECONDARY"; }
                else if (classNameLower.Contains("10")) { level = "class10"; badge = "SECONDARY"; }
                else if (classNameLower.Contains("bachelor") || classNameLower.Contains("bbs")) { level = "bachelors"; badge = "UNIVERSITY"; }

                batchList.Add(new TeacherBatchItemViewModel
                {
                    BatchId = b.BatchId,
                    BatchName = b.BatchName,
                    ClassName = b.Class?.ClassName ?? "General Class",
                    SubjectName = b.Subject?.SubjectName ?? "General Subject",
                    TimeSlot = b.TimeSlot?.Days ?? "7:00 AM - 8:30 AM",
                    Capacity = b.Capacity > 0 ? b.Capacity : 40,
                    EnrolledStudentsCount = b.EnrollmentSubjects.Count(),
                    AcademicLevel = level,
                    AcademicLevelBadge = badge,
                    Shift = b.BatchId % 2 == 0 ? "evening" : "morning",
                    IsActive = b.IsActive
                });
            }

            var viewModel = new TeacherBatchManagementViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "",
                Batches = batchList
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Classes()
        {
            return RedirectToAction("ClassManagement");
        }

        [HttpGet]
        public IActionResult LiveClasses()
        {
            var (teacherId, teacher) = GetCurrentTeacher();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var liveSessions = _context.ClassSessions
                .Include(cs => cs.Batch).ThenInclude(b => b.Subject)
                .Include(cs => cs.Batch).ThenInclude(b => b.Class)
                .Include(cs => cs.Batch).ThenInclude(b => b.EnrollmentSubjects)
                .Where(cs => cs.TeacherId == teacherId && cs.SessionDate == today)
                .ToList();

            var activeList = new List<UpcomingClassViewModel>();
            var upcomingList = new List<UpcomingClassViewModel>();

            var now = TimeOnly.FromDateTime(DateTime.Now);
            foreach (var s in liveSessions)
            {
                var item = new UpcomingClassViewModel
                {
                    SessionId = s.SessionId,
                    Title = s.Title,
                    BatchName = s.Batch?.BatchName ?? "Batch",
                    SubjectName = s.Batch?.Subject?.SubjectName ?? "Subject",
                    ClassName = s.Batch?.Class?.ClassName ?? "Class",
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    EnrolledStudents = s.Batch?.EnrollmentSubjects.Count ?? 0,
                    MeetingLink = string.IsNullOrEmpty(s.MeetingLink) ? "https://meet.google.com/new" : s.MeetingLink
                };

                if (s.Status == "Live" || (now >= s.StartTime && now <= s.EndTime))
                {
                    activeList.Add(item);
                }
                else
                {
                    upcomingList.Add(item);
                }
            }

            var viewModel = new LiveClassesViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "",
                ActiveLiveSessions = activeList,
                UpcomingLiveSessions = upcomingList
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var (teacherId, teacher) = GetCurrentTeacher();

            var teacherBatches = _context.Batches
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .Include(b => b.EnrollmentSubjects)
                .Where(b => b.TeacherId == teacherId && b.IsActive)
                .ToList();

            int currentLoad = teacherBatches.Count;
            int totalStudents = teacherBatches.Sum(b => b.EnrollmentSubjects.Count);

            var assignedGroups = teacherBatches
                .GroupBy(b => b.Class?.ClassName ?? "General Class")
                .Select(g => new AssignedSubjectGroup
                {
                    ClassName = g.Key,
                    Subjects = string.Join(", ", g.Select(b => b.Subject?.SubjectName).Where(s => !string.IsNullOrEmpty(s)).Distinct())
                })
                .ToList();

            var teachingLevels = teacherBatches
                .Select(b => b.Class?.ClassName)
                .Where(c => !string.IsNullOrEmpty(c))
                .Distinct()
                .Cast<string>()
                .ToList();

            var viewModel = new TeacherProfileViewModel
            {
                FullName = teacher?.FullName ?? "Teacher Profile",
                Email = teacher?.Email ?? "",
                Phone = teacher?.Phone ?? "",
                DateOfBirth = "",
                HighestDegree = "Faculty Member",
                Institution = "Study Point Center",
                YearOfGraduation = "",
                SubjectSpecialization = teacherBatches.FirstOrDefault()?.Subject?.SubjectName ?? "Faculty Educator",
                YearsOfExperience = "Experienced Educator",
                Bio = "Dedicated educator providing instruction and support for enrolled students at Study Point.",
                PhotoPath = teacher?.ProfilePictureUrl ?? "",
                CurrentLoad = currentLoad,
                TotalStudents = totalStudents,
                Rating = 5.0m,
                IsVerified = true,
                TeachingLevels = teachingLevels.Any() ? teachingLevels : new List<string> { "Enrolled Courses" },
                AssignedSubjects = assignedGroups
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(TeacherProfileViewModel model, IFormFile? photoFile)
        {
            var (teacherId, teacher) = GetCurrentTeacher();
            if (teacher != null)
            {
                if (!string.IsNullOrWhiteSpace(model.FullName))
                {
                    teacher.FullName = model.FullName;
                }
                teacher.Phone = model.Phone ?? teacher.Phone;

                if (photoFile != null && photoFile.Length > 0)
                {
                    var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "profiles");
                    if (!Directory.Exists(uploadsFolder))
                    {
                        Directory.CreateDirectory(uploadsFolder);
                    }

                    var extension = Path.GetExtension(photoFile.FileName).ToLower();
                    if (string.IsNullOrEmpty(extension) || (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".gif" && extension != ".webp"))
                    {
                        extension = ".jpg";
                    }

                    var filePath = Path.Combine(uploadsFolder, $"user_{teacher.UserId}{extension}");
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    teacher.ProfileImage = $"/uploads/profiles/user_{teacher.UserId}{extension}";
                }

                _context.SaveChanges();

                // Refresh auth claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, teacher.FullName),
                    new Claim(ClaimTypes.NameIdentifier, teacher.UserId.ToString()),
                    new Claim(ClaimTypes.Role, teacher.Role),
                    new Claim("Email", teacher.Email),
                    new Claim("Phone", teacher.Phone ?? "")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["SuccessMessage"] = "Profile details updated successfully!";
            }
            return RedirectToAction("Profile");
        }

        [HttpGet]
        public IActionResult Students(int? batchId)
        {
            var (teacherId, teacher) = GetCurrentTeacher();

            var dbBatches = _context.Batches
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .Where(b => b.TeacherId == teacherId)
                .ToList();

            var teacherBatches = dbBatches.Select(b => new TeacherBatchItemViewModel
            {
                BatchId = b.BatchId,
                BatchName = b.BatchName,
                ClassName = b.Class?.ClassName ?? "Class",
                SubjectName = b.Subject?.SubjectName ?? "Subject"
            }).ToList();

            // Fetch real enrolled students from database
            var enrollmentSubjectsQuery = _context.EnrollmentSubjects
                .Include(es => es.Enrollment).ThenInclude(e => e.Student)
                .Include(es => es.AssignedBatch).ThenInclude(b => b!.Class)
                .Include(es => es.AssignedBatch).ThenInclude(b => b!.Subject)
                .Where(es => es.AssignedBatch != null && es.AssignedBatch.TeacherId == teacherId);

            if (batchId.HasValue && batchId.Value > 0)
            {
                enrollmentSubjectsQuery = enrollmentSubjectsQuery.Where(es => es.AssignedBatchId == batchId.Value);
            }

            var enrolledItems = enrollmentSubjectsQuery.ToList();

            var teacherSessionIds = _context.ClassSessions
                .Where(cs => cs.TeacherId == teacherId)
                .Select(cs => cs.SessionId)
                .ToList();

            var attendanceRecords = teacherSessionIds.Any()
                ? _context.Attendances.Where(a => teacherSessionIds.Contains(a.SessionId)).ToList()
                : new List<Attendance>();

            var studentsList = enrolledItems
                .Where(es => es.Enrollment?.Student != null)
                .Select(es =>
                {
                    var st = es.Enrollment.Student;
                    var b = es.AssignedBatch;
                    var studentAttendances = attendanceRecords.Where(a => a.StudentId == st.UserId).ToList();
                    double attPct = 100.0;
                    if (studentAttendances.Any())
                    {
                        attPct = Math.Round((double)studentAttendances.Count(a => a.IsPresent) / studentAttendances.Count * 100, 1);
                    }

                    return new TeacherStudentItemViewModel
                    {
                        StudentId = st.UserId,
                        FullName = st.FullName,
                        Email = st.Email,
                        Phone = st.Phone ?? "",
                        BatchName = b?.BatchName ?? "Batch",
                        ClassName = b?.Class?.ClassName ?? "Class",
                        SubjectName = b?.Subject?.SubjectName ?? "Subject",
                        EnrollmentDate = es.Enrollment.EnrolledDate ?? DateTime.Now,
                        AttendancePercentage = attPct,
                        Status = (st.IsActive ?? true) ? "Active" : "Inactive"
                    };
                })
                .GroupBy(s => s.StudentId)
                .Select(g => g.First())
                .ToList();

            var viewModel = new TeacherStudentsViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "",
                SelectedBatchId = batchId ?? 0,
                TeacherBatches = teacherBatches,
                Students = studentsList
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Attendance(int? sessionId)
        {
            var (teacherId, teacher) = GetCurrentTeacher();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var dbSessions = _context.ClassSessions
                .Include(cs => cs.Batch).ThenInclude(b => b.Subject)
                .Include(cs => cs.Batch).ThenInclude(b => b.Class)
                .Where(cs => cs.TeacherId == teacherId)
                .OrderByDescending(cs => cs.SessionDate)
                .ThenBy(cs => cs.StartTime)
                .ToList();

            var teacherSessionVMs = dbSessions.Select(s => new UpcomingClassViewModel
            {
                SessionId = s.SessionId,
                Title = s.Title,
                BatchName = s.Batch?.BatchName ?? "Batch",
                SubjectName = s.Batch?.Subject?.SubjectName ?? "Subject",
                ClassName = s.Batch?.Class?.ClassName ?? "Class",
                StartTime = s.StartTime,
                EndTime = s.EndTime
            }).ToList();

            var selectedSession = dbSessions.FirstOrDefault(s => s.SessionId == sessionId) ?? dbSessions.FirstOrDefault();

            var enrolledStudents = new List<AttendanceStudentItemViewModel>();

            if (selectedSession != null)
            {
                var existingAttendances = _context.Attendances
                    .Where(a => a.SessionId == selectedSession.SessionId)
                    .ToList();

                var batchStudents = _context.EnrollmentSubjects
                    .Include(es => es.Enrollment).ThenInclude(e => e.Student)
                    .Where(es => es.AssignedBatchId == selectedSession.BatchId && es.Enrollment != null && es.Enrollment.Student != null)
                    .Select(es => es.Enrollment.Student)
                    .Distinct()
                    .ToList();

                enrolledStudents = batchStudents.Select((st, idx) =>
                {
                    var att = existingAttendances.FirstOrDefault(a => a.StudentId == st.UserId);
                    return new AttendanceStudentItemViewModel
                    {
                        StudentId = st.UserId,
                        FullName = st.FullName,
                        Email = st.Email,
                        RollNo = $"STU-{st.UserId:D3}",
                        IsPresent = att?.IsPresent ?? true
                    };
                }).ToList();
            }

            var viewModel = new TeacherAttendanceViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "",
                SelectedSessionId = selectedSession?.SessionId ?? 0,
                SessionTitle = selectedSession?.Title ?? "No Active Session",
                BatchName = selectedSession?.Batch?.BatchName ?? "No Batch",
                SessionDate = selectedSession?.SessionDate ?? today,
                TeacherSessions = teacherSessionVMs,
                EnrolledStudents = enrolledStudents
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult MarkAttendance([FromBody] MarkAttendanceRequest request)
        {
            try
            {
                if (request != null && request.SessionId > 0 && request.AttendanceList != null)
                {
                    foreach (var item in request.AttendanceList)
                    {
                        var attendanceRecord = _context.Attendances.FirstOrDefault(a => a.SessionId == request.SessionId && a.StudentId == item.StudentId);
                        if (attendanceRecord == null)
                        {
                            attendanceRecord = new Attendance
                            {
                                SessionId = request.SessionId,
                                StudentId = item.StudentId,
                                IsPresent = item.IsPresent,
                                MarkedDate = DateTime.Now
                            };
                            _context.Attendances.Add(attendanceRecord);
                        }
                        else
                        {
                            attendanceRecord.IsPresent = item.IsPresent;
                            attendanceRecord.MarkedDate = DateTime.Now;
                        }
                    }
                    _context.SaveChanges();
                    return Json(new { success = true, message = $"Attendance marked successfully in database for {request.AttendanceList.Count} students!" });
                }
                return Json(new { success = false, message = "Invalid request payload." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult UpdateMeetingLink([FromBody] UpdateMeetingLinkRequest request)
        {
            try
            {
                if (request.SessionId > 0)
                {
                    var session = _context.ClassSessions.FirstOrDefault(s => s.SessionId == request.SessionId);
                    if (session != null)
                    {
                        session.MeetingLink = request.MeetingLink;
                        _context.SaveChanges();
                    }
                }
                return Json(new { success = true, message = "Meeting link updated successfully!" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpPost]
        public IActionResult CreateClass([FromBody] CreateClassRequest request)
        {
            try
            {
                var (teacherId, teacher) = GetCurrentTeacher();
                if (teacherId == 0) return Unauthorized("User is not logged in.");

                var batch = _context.Batches.FirstOrDefault(b => b.TeacherId == teacherId);
                if (batch == null)
                {
                    var dummyClass = _context.Classes.FirstOrDefault() ?? new Class { ClassName = request.ClassName.Length > 0 ? request.ClassName : "Class 10" };
                    if (dummyClass.ClassId == 0) _context.Classes.Add(dummyClass);

                    var dummySubject = _context.Subjects.FirstOrDefault() ?? new Subject { SubjectName = request.SubjectName.Length > 0 ? request.SubjectName : "Science", Class = dummyClass };
                    if (dummySubject.SubjectId == 0) _context.Subjects.Add(dummySubject);

                    var dummyCourseType = _context.CourseTypes.FirstOrDefault() ?? new CourseType { TypeName = "Regular" };
                    if (dummyCourseType.CourseTypeId == 0) _context.CourseTypes.Add(dummyCourseType);

                    var dummyTimeSlot = _context.TimeSlots.FirstOrDefault() ?? new TimeSlot { Days = "Mon-Fri" };
                    if (dummyTimeSlot.TimeSlotId == 0) _context.TimeSlots.Add(dummyTimeSlot);

                    _context.SaveChanges();

                    batch = new Batch
                    {
                        BatchName = $"{request.ClassName} - {request.SubjectName}",
                        TeacherId = teacherId,
                        ClassId = dummyClass.ClassId,
                        SubjectId = dummySubject.SubjectId,
                        CourseTypeId = dummyCourseType.CourseTypeId,
                        TimeSlotId = dummyTimeSlot.TimeSlotId,
                        Capacity = 35
                    };
                    _context.Batches.Add(batch);
                    _context.SaveChanges();
                }

                if (!TimeOnly.TryParse(request.StartTime, out TimeOnly startTime)) startTime = new TimeOnly(10, 0);
                if (!TimeOnly.TryParse(request.EndTime, out TimeOnly endTime)) endTime = new TimeOnly(11, 30);
                if (!DateTime.TryParse(request.StartDate, out DateTime startDate)) startDate = DateTime.Today;
                if (!DateTime.TryParse(request.EndDate, out DateTime endDate)) endDate = startDate.AddMonths(1);

                var dayMap = new Dictionary<string, DayOfWeek>
                {
                    { "SUN", DayOfWeek.Sunday }, { "MON", DayOfWeek.Monday }, { "TUE", DayOfWeek.Tuesday },
                    { "WED", DayOfWeek.Wednesday }, { "THU", DayOfWeek.Thursday }, { "FRI", DayOfWeek.Friday }, { "SAT", DayOfWeek.Saturday }
                };

                var selectedDays = new HashSet<DayOfWeek>();
                foreach (var dayStr in request.Days)
                {
                    if (dayMap.TryGetValue(dayStr.ToUpper(), out DayOfWeek dow)) selectedDays.Add(dow);
                }

                var title = $"{request.SubjectName} - {request.ClassName}";
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    if (!selectedDays.Any() || selectedDays.Contains(date.DayOfWeek))
                    {
                        _context.ClassSessions.Add(new ClassSession
                        {
                            BatchId = batch.BatchId,
                            TeacherId = teacherId,
                            Title = title,
                            MeetingLink = "https://meet.google.com/new",
                            SessionDate = DateOnly.FromDateTime(date),
                            StartTime = startTime,
                            EndTime = endTime,
                            Status = "Upcoming"
                        });
                    }
                }

                _context.SaveChanges();
                return Json(new { success = true, message = "Class schedule created successfully!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }

    public class CreateClassRequest
    {
        public string ClassName { get; set; } = string.Empty;
        public string SubjectName { get; set; } = string.Empty;
        public string StartDate { get; set; } = string.Empty;
        public string EndDate { get; set; } = string.Empty;
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public List<string> Days { get; set; } = new();
    }
}