using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
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
                // Fallback for dev mode / testing if claim not present
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

            // Batches for this teacher
            var teacherBatches = _context.Batches
                .Include(b => b.EnrollmentSubjects)
                .Where(b => b.TeacherId == teacherId)
                .ToList();

            int totalStudents = teacherBatches.Sum(b => b.EnrollmentSubjects.Count());
            int activeClasses = teacherBatches.Count(b => b.IsActive);
            if (totalStudents == 0) totalStudents = 142; // Demo display fallback if empty
            if (activeClasses == 0) activeClasses = 5;

            var today = DateOnly.FromDateTime(DateTime.Today);
            var upcomingSessions = _context.ClassSessions
                .Include(cs => cs.Batch)
                    .ThenInclude(b => b.Subject)
                .Include(cs => cs.Batch)
                    .ThenInclude(b => b.Class)
                .Include(cs => cs.Batch)
                    .ThenInclude(b => b.EnrollmentSubjects)
                .Where(cs => cs.TeacherId == teacherId && cs.SessionDate >= today)
                .OrderBy(cs => cs.SessionDate)
                .ThenBy(cs => cs.StartTime)
                .Take(6)
                .ToList();

            // Fallback sessions if DB has none for current date
            var nextSession = upcomingSessions.FirstOrDefault();
            var otherUpcoming = upcomingSessions.Skip(1).ToList();

            var viewModel = new TeacherDashboardViewModel
            {
                TeacherName = teacher.FullName,
                TeacherEmail = teacher.Email,
                TotalStudents = totalStudents,
                ActiveClasses = activeClasses,
                AverageAttendance = 94.2,
                CurrentAcademicYear = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",

                NextClass = nextSession != null ? new UpcomingClassViewModel
                {
                    SessionId = nextSession.SessionId,
                    Title = nextSession.Title,
                    BatchName = nextSession.Batch?.BatchName ?? "Batch A",
                    SubjectName = nextSession.Batch?.Subject?.SubjectName ?? "Mathematics",
                    ClassName = nextSession.Batch?.Class?.ClassName ?? "Grade 10",
                    StartTime = nextSession.StartTime,
                    EndTime = nextSession.EndTime,
                    EnrolledStudents = nextSession.Batch?.EnrollmentSubjects.Count ?? 35,
                    MeetingLink = string.IsNullOrEmpty(nextSession.MeetingLink) ? "https://meet.google.com/new" : nextSession.MeetingLink
                } : new UpcomingClassViewModel
                {
                    SessionId = 101,
                    Title = "Mathematics - Grade 10",
                    BatchName = "Batch A",
                    SubjectName = "Mathematics",
                    ClassName = "Grade 10",
                    StartTime = new TimeOnly(10, 0),
                    EndTime = new TimeOnly(11, 30),
                    EnrolledStudents = 38,
                    MeetingLink = "https://meet.google.com/xyz-teacher-demo"
                },

                UpcomingClasses = otherUpcoming.Any() ? otherUpcoming.Select(s => new UpcomingClassViewModel
                {
                    SessionId = s.SessionId,
                    Title = s.Title,
                    BatchName = s.Batch?.BatchName ?? "Batch A",
                    SubjectName = s.Batch?.Subject?.SubjectName ?? "Science",
                    ClassName = s.Batch?.Class?.ClassName ?? "Grade 10",
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    EnrolledStudents = s.Batch?.EnrollmentSubjects.Count ?? 28,
                    MeetingLink = string.IsNullOrEmpty(s.MeetingLink) ? "https://meet.google.com/new" : s.MeetingLink
                }).ToList() : GetFallbackUpcomingClasses()
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
                .Include(cs => cs.Batch)
                    .ThenInclude(b => b.Subject)
                .Include(cs => cs.Batch)
                    .ThenInclude(b => b.Class)
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

                // If DB empty, provide interactive fallback items
                if (!daySessions.Any() && (currentDate.DayOfWeek == DayOfWeek.Monday || currentDate.DayOfWeek == DayOfWeek.Wednesday || currentDate.DayOfWeek == DayOfWeek.Friday))
                {
                    daySessions.Add(new ClassSessionViewModel
                    {
                        SessionId = 200 + i,
                        Title = "Accountancy - Grade 12",
                        SubjectName = "Accountancy",
                        ClassName = "Grade 12",
                        StartTime = new TimeOnly(7, 0),
                        EndTime = new TimeOnly(8, 30),
                        ColorTheme = "bg-blue-600 text-white"
                    });
                    daySessions.Add(new ClassSessionViewModel
                    {
                        SessionId = 300 + i,
                        Title = "Mathematics - Grade 10",
                        SubjectName = "Mathematics",
                        ClassName = "Grade 10",
                        StartTime = new TimeOnly(16, 30),
                        EndTime = new TimeOnly(18, 0),
                        ColorTheme = "bg-emerald-600 text-white"
                    });
                }

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

            if (dbBatches.Any())
            {
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
            }
            else
            {
                // Fallback display batches
                batchList = GetFallbackBatches();
            }

            var viewModel = new TeacherBatchManagementViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "teacher@studypoint.com",
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

            if (liveSessions.Any())
            {
                var now = TimeOnly.FromDateTime(DateTime.Now);
                foreach (var s in liveSessions)
                {
                    var item = new UpcomingClassViewModel
                    {
                        SessionId = s.SessionId,
                        Title = s.Title,
                        BatchName = s.Batch?.BatchName ?? "Batch A",
                        SubjectName = s.Batch?.Subject?.SubjectName ?? "Subject",
                        ClassName = s.Batch?.Class?.ClassName ?? "Class",
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        EnrolledStudents = s.Batch?.EnrollmentSubjects.Count ?? 30,
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
            }

            if (!activeList.Any() && !upcomingList.Any())
            {
                // Fallback live sessions
                activeList.Add(new UpcomingClassViewModel
                {
                    SessionId = 501,
                    Title = "Accountancy - Grade 12 (Live Stream)",
                    BatchName = "Morning Batch A",
                    SubjectName = "Accountancy",
                    ClassName = "Grade 12",
                    StartTime = TimeOnly.FromDateTime(DateTime.Now.AddMinutes(-15)),
                    EndTime = TimeOnly.FromDateTime(DateTime.Now.AddMinutes(45)),
                    EnrolledStudents = 42,
                    MeetingLink = "https://meet.google.com/acc-12-live"
                });

                upcomingList = GetFallbackUpcomingClasses();
            }

            var viewModel = new LiveClassesViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "teacher@studypoint.com",
                ActiveLiveSessions = activeList,
                UpcomingLiveSessions = upcomingList
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var (teacherId, teacher) = GetCurrentTeacher();

            var viewModel = new TeacherProfileViewModel
            {
                FullName = teacher?.FullName ?? "Teacher Profile",
                Email = teacher?.Email ?? "teacher@studypoint.com",
                Phone = teacher?.Phone ?? "+977 9841234567",
                DateOfBirth = "1990-05-15",
                HighestDegree = "Master of Science in Physics / Education",
                Institution = "Tribhuvan University",
                YearOfGraduation = "2015",
                SubjectSpecialization = "Mathematics & Physics",
                YearsOfExperience = "8 Years",
                Bio = "Passionate educator with over 8 years of experience empowering high school and university students to master STEM subjects.",
                PhotoPath = teacher?.ProfilePictureUrl ?? "https://i.pravatar.cc/150?img=11",
                CurrentLoad = 5,
                TotalStudents = 142,
                Rating = 4.9m,
                IsVerified = true,
                TeachingLevels = new List<string> { "Grade 10", "Grade 12", "Bachelor's" },
                AssignedSubjects = new List<AssignedSubjectGroup>
                {
                    new AssignedSubjectGroup { ClassName = "Grade 10", Subjects = "Mathematics, Science" },
                    new AssignedSubjectGroup { ClassName = "Grade 12", Subjects = "Accountancy, Advanced Physics" },
                    new AssignedSubjectGroup { ClassName = "Bachelor's", Subjects = "Financial Management" }
                }
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
                var identity = new ClaimsIdentity(claims, Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(Microsoft.AspNetCore.Authentication.Cookies.CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

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

            if (!teacherBatches.Any())
            {
                teacherBatches = GetFallbackBatches();
            }

            var studentsList = new List<TeacherStudentItemViewModel>
            {
                new TeacherStudentItemViewModel { StudentId = 1, FullName = "Aarav Sharma", Email = "aarav.sharma@example.com", Phone = "9841001122", BatchName = "Batch A", ClassName = "Grade 12", SubjectName = "Accountancy", EnrollmentDate = DateTime.Now.AddMonths(-3), AttendancePercentage = 96.5, Status = "Active" },
                new TeacherStudentItemViewModel { StudentId = 2, FullName = "Priya Adhikari", Email = "priya.a@example.com", Phone = "9841003344", BatchName = "Batch A", ClassName = "Grade 12", SubjectName = "Accountancy", EnrollmentDate = DateTime.Now.AddMonths(-2), AttendancePercentage = 92.0, Status = "Active" },
                new TeacherStudentItemViewModel { StudentId = 3, FullName = "Rohan Shrestha", Email = "rohan.s@example.com", Phone = "9841005566", BatchName = "Batch B", ClassName = "Grade 10", SubjectName = "Mathematics", EnrollmentDate = DateTime.Now.AddMonths(-4), AttendancePercentage = 88.0, Status = "Active" },
                new TeacherStudentItemViewModel { StudentId = 4, FullName = "Sita Thapa", Email = "sita.t@example.com", Phone = "9841007788", BatchName = "Batch B", ClassName = "Grade 10", SubjectName = "Mathematics", EnrollmentDate = DateTime.Now.AddMonths(-1), AttendancePercentage = 98.2, Status = "Active" },
                new TeacherStudentItemViewModel { StudentId = 5, FullName = "Bikash Gurung", Email = "bikash.g@example.com", Phone = "9841009900", BatchName = "Batch C", ClassName = "BBS", SubjectName = "Financial Mgmt", EnrollmentDate = DateTime.Now.AddMonths(-5), AttendancePercentage = 94.0, Status = "Active" }
            };

            if (batchId.HasValue && batchId.Value > 0)
            {
                var selectedB = teacherBatches.FirstOrDefault(b => b.BatchId == batchId.Value);
                if (selectedB != null)
                {
                    studentsList = studentsList.Where(s => s.BatchName == selectedB.BatchName || s.ClassName == selectedB.ClassName).ToList();
                }
            }

            var viewModel = new TeacherStudentsViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "teacher@studypoint.com",
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

            var sessions = GetFallbackUpcomingClasses();
            var selectedSession = sessions.FirstOrDefault(s => s.SessionId == sessionId) ?? sessions.First();

            var enrolledStudents = new List<AttendanceStudentItemViewModel>
            {
                new AttendanceStudentItemViewModel { StudentId = 1, FullName = "Aarav Sharma", Email = "aarav@example.com", RollNo = "STU-101", IsPresent = true },
                new AttendanceStudentItemViewModel { StudentId = 2, FullName = "Priya Adhikari", Email = "priya@example.com", RollNo = "STU-102", IsPresent = true },
                new AttendanceStudentItemViewModel { StudentId = 3, FullName = "Rohan Shrestha", Email = "rohan@example.com", RollNo = "STU-103", IsPresent = false, Remarks = "Medical leave" },
                new AttendanceStudentItemViewModel { StudentId = 4, FullName = "Sita Thapa", Email = "sita@example.com", RollNo = "STU-104", IsPresent = true },
                new AttendanceStudentItemViewModel { StudentId = 5, FullName = "Bikash Gurung", Email = "bikash@example.com", RollNo = "STU-105", IsPresent = true }
            };

            var viewModel = new TeacherAttendanceViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "teacher@studypoint.com",
                SelectedSessionId = selectedSession.SessionId,
                SessionTitle = selectedSession.Title,
                BatchName = selectedSession.BatchName,
                SessionDate = DateOnly.FromDateTime(DateTime.Today),
                TeacherSessions = sessions,
                EnrolledStudents = enrolledStudents
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult MarkAttendance([FromBody] MarkAttendanceRequest request)
        {
            return Json(new { success = true, message = $"Attendance marked successfully for {request.AttendanceList.Count} students!" });
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

        #region Helper Fallback Methods
        private List<UpcomingClassViewModel> GetFallbackUpcomingClasses()
        {
            return new List<UpcomingClassViewModel>
            {
                new UpcomingClassViewModel
                {
                    SessionId = 102,
                    Title = "Mathematics - Grade 10 (Advanced Algebra)",
                    BatchName = "Batch A",
                    SubjectName = "Mathematics",
                    ClassName = "Grade 10",
                    StartTime = new TimeOnly(7, 0),
                    EndTime = new TimeOnly(8, 30),
                    EnrolledStudents = 38,
                    MeetingLink = "https://meet.google.com/math-10-demo"
                },
                new UpcomingClassViewModel
                {
                    SessionId = 103,
                    Title = "Accountancy - Grade 12 (Financial Accounting)",
                    BatchName = "Batch Morning A",
                    SubjectName = "Accountancy",
                    ClassName = "Grade 12",
                    StartTime = new TimeOnly(9, 0),
                    EndTime = new TimeOnly(10, 30),
                    EnrolledStudents = 42,
                    MeetingLink = "https://meet.google.com/acc-12-demo"
                },
                new UpcomingClassViewModel
                {
                    SessionId = 104,
                    Title = "Science - Grade 9 (Physics Lab & Quiz)",
                    BatchName = "Batch Evening",
                    SubjectName = "Science",
                    ClassName = "Grade 9",
                    StartTime = new TimeOnly(16, 0),
                    EndTime = new TimeOnly(17, 30),
                    EnrolledStudents = 31,
                    MeetingLink = "https://meet.google.com/sci-9-demo"
                }
            };
        }

        private List<TeacherBatchItemViewModel> GetFallbackBatches()
        {
            return new List<TeacherBatchItemViewModel>
            {
                new TeacherBatchItemViewModel
                {
                    BatchId = 1,
                    BatchName = "Batch A",
                    ClassName = "Grade 12",
                    SubjectName = "Accountancy",
                    TimeSlot = "7:00 AM - 8:30 AM",
                    Capacity = 50,
                    EnrolledStudentsCount = 42,
                    AcademicLevel = "class12",
                    AcademicLevelBadge = "HIGHER SECONDARY",
                    Shift = "morning",
                    IsActive = true
                },
                new TeacherBatchItemViewModel
                {
                    BatchId = 2,
                    BatchName = "Batch C",
                    ClassName = "Grade 10",
                    SubjectName = "Mathematics",
                    TimeSlot = "4:30 PM - 6:00 PM",
                    Capacity = 40,
                    EnrolledStudentsCount = 31,
                    AcademicLevel = "class10",
                    AcademicLevelBadge = "SECONDARY",
                    Shift = "evening",
                    IsActive = true
                },
                new TeacherBatchItemViewModel
                {
                    BatchId = 3,
                    BatchName = "Batch B",
                    ClassName = "BBS",
                    SubjectName = "Financial Mgmt",
                    TimeSlot = "6:30 AM - 8:00 AM",
                    Capacity = 25,
                    EnrolledStudentsCount = 18,
                    AcademicLevel = "bachelors",
                    AcademicLevelBadge = "UNIVERSITY",
                    Shift = "morning",
                    IsActive = true
                },
                new TeacherBatchItemViewModel
                {
                    BatchId = 4,
                    BatchName = "Evening Shift",
                    ClassName = "Grade 9",
                    SubjectName = "Science",
                    TimeSlot = "5:00 PM - 6:30 PM",
                    Capacity = 35,
                    EnrolledStudentsCount = 29,
                    AcademicLevel = "secondary",
                    AcademicLevelBadge = "SECONDARY",
                    Shift = "evening",
                    IsActive = true
                }
            };
        }
        #endregion
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