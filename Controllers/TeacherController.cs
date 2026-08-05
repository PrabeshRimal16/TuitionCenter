using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using TuitionCenter.Models;
using TuitionCenter.Models.ViewModels;
using System.Globalization;

namespace TuitionCenter.Controllers
{
    public class TeacherController : Controller
    {
        private readonly TuitionCenterDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public TeacherController(TuitionCenterDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int teacherId))
            {
                return RedirectToAction("Login", "Account");
            }

            var teacher = _context.Users.FirstOrDefault(u => u.UserId == teacherId);
            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Calculate stats
            var teacherBatches = _context.Batches
                .Include(b => b.EnrollmentSubjects)
                .Where(b => b.TeacherId == teacherId)
                .ToList();

            int totalStudents = teacherBatches.Sum(b => b.EnrollmentSubjects.Count());
            int activeClasses = teacherBatches.Count(b => b.IsActive);

            // Get upcoming classes (sessions for today or future, ordered by Date and Time)
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
                .Take(5)
                .ToList();

            var nextSession = upcomingSessions.FirstOrDefault();
            var otherUpcoming = upcomingSessions.Skip(1).ToList();

            var viewModel = new TeacherDashboardViewModel
            {
                TeacherName = teacher.FullName,
                TeacherEmail = teacher.Email,
                TotalStudents = totalStudents,
                ActiveClasses = activeClasses,
                AverageAttendance = 0, // Placeholder
                CurrentAcademicYear = $"{DateTime.Now.Year}-{DateTime.Now.Year + 1}",
                
                NextClass = nextSession != null ? new UpcomingClassViewModel
                {
                    SessionId = nextSession.SessionId,
                    Title = nextSession.Title,
                    BatchName = nextSession.Batch.BatchName,
                    SubjectName = nextSession.Batch.Subject.SubjectName,
                    ClassName = nextSession.Batch.Class.ClassName,
                    StartTime = nextSession.StartTime,
                    EndTime = nextSession.EndTime,
                    EnrolledStudents = nextSession.Batch.EnrollmentSubjects.Count(),
                    MeetingLink = nextSession.MeetingLink
                } : null,

                UpcomingClasses = otherUpcoming.Select(s => new UpcomingClassViewModel
                {
                    SessionId = s.SessionId,
                    Title = s.Title,
                    BatchName = s.Batch.BatchName,
                    SubjectName = s.Batch.Subject.SubjectName,
                    ClassName = s.Batch.Class.ClassName,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                    EnrolledStudents = s.Batch.EnrollmentSubjects.Count(),
                    MeetingLink = s.MeetingLink
                }).ToList()
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Schedule(DateTime? startDate)
        {
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(userIdString, out int teacherId))
            {
                return RedirectToAction("Login", "Account");
            }

            var teacher = _context.Users.FirstOrDefault(u => u.UserId == teacherId);
            if (teacher == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Determine the start of the week (Sunday)
            DateTime start = startDate ?? DateTime.Today;
            int diff = (7 + (start.DayOfWeek - DayOfWeek.Sunday)) % 7;
            DateTime startOfWeek = start.AddDays(-1 * diff).Date;
            DateTime endOfWeek = startOfWeek.AddDays(6).Date;

            // Fetch sessions for the week
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

            string[] colorThemes = { "bg-blue-500 text-white", "bg-green-500 text-white", "bg-indigo-500 text-white", "bg-teal-500 text-white" };

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
                        SubjectName = s.Batch.Subject.SubjectName,
                        ClassName = s.Batch.Class.ClassName,
                        StartTime = s.StartTime,
                        EndTime = s.EndTime,
                        ColorTheme = colorThemes[s.Batch.SubjectId % colorThemes.Length] // Pseudo-random color based on subject
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
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int teacherId = 0;
            int.TryParse(userIdString, out teacherId);
            var teacher = _context.Users.FirstOrDefault(u => u.UserId == teacherId);

            var viewModel = new TeacherDashboardViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "teacher@studypoint.com"
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
            var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int teacherId = 0;
            int.TryParse(userIdString, out teacherId);
            var teacher = _context.Users.FirstOrDefault(u => u.UserId == teacherId);

            var viewModel = new TeacherDashboardViewModel
            {
                TeacherName = teacher?.FullName ?? "Teacher",
                TeacherEmail = teacher?.Email ?? "teacher@studypoint.com"
            };

            return View(viewModel);
        }

        [HttpPost]
        public IActionResult CreateClass([FromBody] CreateClassRequest request)
        {
            try 
            {
                var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!int.TryParse(userIdString, out int teacherId))
                {
                    return Unauthorized("User is not logged in.");
                }

                // Find a valid batch for the teacher or fallback
                var batch = _context.Batches.FirstOrDefault(b => b.TeacherId == teacherId);
                
                if (batch == null) 
                {
                    // Fallback: Create a dummy batch if none exists
                    var dummyClass = _context.Classes.FirstOrDefault() ?? new Class { ClassName = "Class 10" };
                    if (dummyClass.ClassId == 0) _context.Classes.Add(dummyClass);

                    var dummySubject = _context.Subjects.FirstOrDefault() ?? new Subject { SubjectName = "Science", Class = dummyClass };
                    if (dummySubject.SubjectId == 0) _context.Subjects.Add(dummySubject);

                    var dummyCourseType = _context.CourseTypes.FirstOrDefault() ?? new CourseType { CourseTypeName = "Regular" };
                    if (dummyCourseType.CourseTypeId == 0) _context.CourseTypes.Add(dummyCourseType);

                    var dummyTimeSlot = _context.TimeSlots.FirstOrDefault() ?? new TimeSlot { Days = "Mon-Fri" };
                    if (dummyTimeSlot.TimeSlotId == 0) _context.TimeSlots.Add(dummyTimeSlot);

                    _context.SaveChanges();

                    batch = new Batch {
                        BatchName = "Default Batch",
                        TeacherId = teacherId,
                        ClassId = dummyClass.ClassId,
                        SubjectId = dummySubject.SubjectId,
                        CourseTypeId = dummyCourseType.CourseTypeId,
                        TimeSlotId = dummyTimeSlot.TimeSlotId,
                        Capacity = 30
                    };
                    _context.Batches.Add(batch);
                    _context.SaveChanges();
                }

                // Parse time
                if (!TimeOnly.TryParse(request.StartTime, out TimeOnly startTime)) startTime = new TimeOnly(10, 0);
                if (!TimeOnly.TryParse(request.EndTime, out TimeOnly endTime)) endTime = new TimeOnly(11, 0);

                // Parse date range
                if (!DateTime.TryParse(request.StartDate, out DateTime startDate)) startDate = DateTime.Today;
                if (!DateTime.TryParse(request.EndDate, out DateTime endDate)) endDate = startDate.AddMonths(1);

                var dayMap = new Dictionary<string, DayOfWeek>
                {
                    { "SUN", DayOfWeek.Sunday }, { "MON", DayOfWeek.Monday }, { "TUE", DayOfWeek.Tuesday },
                    { "WED", DayOfWeek.Wednesday }, { "THU", DayOfWeek.Thursday }, { "FRI", DayOfWeek.Friday }, { "SAT", DayOfWeek.Saturday }
                };

                // Build set of selected DayOfWeek values
                var selectedDays = new HashSet<DayOfWeek>();
                foreach (var dayStr in request.Days)
                {
                    if (dayMap.TryGetValue(dayStr.ToUpper(), out DayOfWeek dow))
                        selectedDays.Add(dow);
                }

                // Iterate every day in the range and create a session on matching weekdays
                var title = $"{request.SubjectName} - {request.ClassName}";
                for (var date = startDate.Date; date <= endDate.Date; date = date.AddDays(1))
                {
                    if (selectedDays.Contains(date.DayOfWeek))
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
                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return StatusCode(500, ex.InnerException?.Message ?? ex.Message);
            }
        }
    }

    public class CreateClassRequest
    {
        public string ClassName { get; set; }
        public string SubjectName { get; set; }
        public string StartDate { get; set; }
        public string EndDate { get; set; }
        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public List<string> Days { get; set; }
    }
}
