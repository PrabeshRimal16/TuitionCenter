using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;
using TuitionCenter.Models;

namespace TuitionCenter.Controllers
{
    public class StudentController : Controller
    {
        private readonly TuitionCenterDbContext _context;
        private const string CurrentClassSessionKey = "EnrollCurrentClassId";

        public StudentController(TuitionCenterDbContext context)
        {
            _context = context;
        }


        // ============================================================
        // Dashboard
        // ============================================================

        [HttpGet]
        public IActionResult Dashboard()
        {
            var studentId = GetCurrentUserId();
            if (studentId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var student = _context.Users.FirstOrDefault(u => u.UserId == studentId);
            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var enrollments = _context.Enrollments
                .Where(e => e.StudentId == studentId && e.Status == "Approved")
                .Include(e => e.Class)
                .Include(e => e.CourseType)
                .Include(e => e.Payments)
                .Include(e => e.EnrollmentSubjects).ThenInclude(es => es.Subject)
                .Include(e => e.EnrollmentSubjects).ThenInclude(es => es.AssignedBatch).ThenInclude(b => b!.Teacher)
                .Include(e => e.EnrollmentSubjects).ThenInclude(es => es.AssignedBatch).ThenInclude(b => b!.TimeSlot)
                .ToList();

            var batchIds = enrollments
                .SelectMany(e => e.EnrollmentSubjects)
                .Where(es => es.AssignedBatchId != null)
                .Select(es => es.AssignedBatchId!.Value)
                .Distinct()
                .ToList();

            var today = DateOnly.FromDateTime(DateTime.Now);

            var allSessions = _context.ClassSessions
                .Where(s => batchIds.Contains(s.BatchId))
                .Include(s => s.Batch).ThenInclude(b => b.Subject)
                .Include(s => s.Batch).ThenInclude(b => b.Class)
                .Include(s => s.Teacher)
                .OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime)
                .ToList();

            var todaysSessions = allSessions.Where(s => s.SessionDate == today).ToList();
            var upcoming = allSessions
                .Where(s => s.SessionDate >= today && s.Status != "Completed" && s.Status != "Cancelled")
                .Take(3)
                .ToList();

            var announcements = _context.Announcements
                .Where(a => batchIds.Contains(a.BatchId))
                .Include(a => a.Teacher)
                .OrderByDescending(a => a.CreatedDate)
                .Take(3)
                .ToList();

            var totalSessions = allSessions.Count;
            var completedSessions = allSessions.Count(s => s.Status == "Completed");

            var latestPayment = enrollments
                .SelectMany(e => e.Payments)
                .OrderByDescending(p => p.PaymentDate)
                .FirstOrDefault();

            var model = new StudentDashboardViewModel
            {
                StudentName = student.FullName,
                AvatarInitial = string.IsNullOrWhiteSpace(student.FullName) ? "?" : student.FullName.Trim()[0].ToString().ToUpper(),
                Today = DateTime.Now,

                ActiveEnrollmentCount = enrollments.Count,
                ActiveCourseSummary = enrollments.Count == 0
                    ? "No active course"
                    : string.Join(", ", enrollments.Select(e => e.Class.ClassName).Distinct()),

                TodaysSessionCount = todaysSessions.Count,
                TodaysSessionTimeRange = todaysSessions.Count == 0
                    ? "No class today"
                    : $"{todaysSessions[0].StartTime:h:mm tt} to {todaysSessions[0].EndTime:h:mm tt}",

                SessionsCompleted = completedSessions,
                SessionsTotal = totalSessions,

                PaymentStatus = latestPayment == null ? "Not Paid" : latestPayment.Status,
                PaymentAmount = latestPayment?.Amount ?? enrollments.Sum(e => e.ExpectedAmount),

                UpcomingSessions = upcoming.Select(s => new UpcomingSessionViewModel
                {
                    SubjectName = s.Batch.Subject.SubjectName,
                    ClassName = s.Batch.Class.ClassName,
                    Title = s.Title,
                    TeacherName = s.Teacher.FullName,
                    TimeRange = $"{s.StartTime:h:mm tt} to {s.EndTime:h:mm tt}",
                    StartsInLabel = GetStartsInLabel(s.SessionDate, s.StartTime),
                    MeetingLink = s.MeetingLink,
                    IsToday = s.SessionDate == today
                }).ToList(),

                Announcements = announcements.Select(a => new DashboardAnnouncementViewModel
                {
                    Title = a.Title,
                    Description = a.Description,
                    TimeAgo = GetTimeAgo(a.CreatedDate),
                    TeacherName = a.Teacher.FullName
                }).ToList(),

                EnrolledCourses = enrollments.Select(e =>
                {
                    var batchIdsForThisEnrollment = e.EnrollmentSubjects
                        .Where(es => es.AssignedBatchId != null)
                        .Select(es => es.AssignedBatchId!.Value)
                        .ToList();

                    var sessionsForEnrollment = allSessions
                        .Where(s => batchIdsForThisEnrollment.Contains(s.BatchId))
                        .ToList();

                    return new EnrolledCourseViewModel
                    {
                        EnrollmentId = e.EnrollmentId,
                        ClassName = e.Class.ClassName,
                        SubjectsSummary = string.Join(", ", e.EnrollmentSubjects.Select(es => es.Subject.SubjectName)),
                        Amount = e.ExpectedAmount,
                        PlanLabel = e.CourseType.TypeName,
                        SessionsCompleted = sessionsForEnrollment.Count(s => s.Status == "Completed"),
                        SessionsTotal = sessionsForEnrollment.Count
                    };
                }).ToList()
            };

            return View(model);
        }

        // ============================================================
        // Classmates
        // ============================================================

        [HttpGet]
        public IActionResult Classmates(int classId)
        {
            var classEntity = _context.Classes.FirstOrDefault(c => c.ClassId == classId);
            if (classEntity == null) return NotFound();

            var classmates = _context.Enrollments
                .Where(e => e.ClassId == classId && e.Status == "Approved")
                .Include(e => e.Student)
                .OrderBy(e => e.Student.FullName)
                .Select(e => new ClassmateItem
                {
                    Name = e.Student.FullName,
                    Email = e.Student.Email,
                    EnrolledDateLabel = e.EnrolledDate.HasValue ? e.EnrolledDate.Value.ToString("MMM d, yyyy") : "-",
                    Status = e.Status
                })
                .ToList();

            var model = new ClassmatesViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                Classmates = classmates
            };

            return View(model);
        }

        // ============================================================
        // Enroll: My Classes (enrolled-only summary, no browse grid)
        // ============================================================

        [HttpGet]
        public IActionResult EnrollClass()
        {
            var studentId = GetCurrentUserId();

            var myEnrollments = studentId == null
                ? new List<Enrollment>()
                : _context.Enrollments
                    .Where(e => e.StudentId == studentId)
                    .Include(e => e.Class)
                    .Include(e => e.Student)
                    .Include(e => e.EnrollmentSubjects).ThenInclude(es => es.Subject)
                    .ToList();

            var myBatchIds = myEnrollments
                .SelectMany(e => e.EnrollmentSubjects)
                .Where(es => es.AssignedBatchId != null)
                .Select(es => es.AssignedBatchId!.Value)
                .Distinct()
                .ToList();

            var today = DateOnly.FromDateTime(DateTime.Now);

            var mySessions = _context.ClassSessions
                .Where(s => myBatchIds.Contains(s.BatchId))
                .ToList();

            var totalSessions = mySessions.Count;
            var completedSessions = mySessions.Count(s => s.Status == "Completed");

            var model = new ClassOverviewViewModel
            {
                MyEnrolledClassCount = myEnrollments.Count,
                SessionsCompleted = completedSessions,
                SessionsTotal = totalSessions,
                OverallCompletionPercent = totalSessions == 0 ? 0 : (int)(completedSessions * 100.0 / totalSessions),
                UpcomingSessionCount = mySessions.Count(s => s.SessionDate >= today && s.Status != "Completed" && s.Status != "Cancelled"),
                UpcomingSessionLabel = mySessions
                    .Where(s => s.SessionDate >= today && s.Status != "Completed" && s.Status != "Cancelled")
                    .OrderBy(s => s.SessionDate).ThenBy(s => s.StartTime)
                    .Select(s => $"{s.SessionDate:MMM d} at {s.StartTime:h:mm tt}")
                    .FirstOrDefault() ?? "No upcoming session",
                MyClasses = myEnrollments.Select(e =>
                {
                    var batchIdsForThis = e.EnrollmentSubjects
                        .Where(es => es.AssignedBatchId != null)
                        .Select(es => es.AssignedBatchId!.Value)
                        .ToList();

                    var sessionsForThis = mySessions.Where(s => batchIdsForThis.Contains(s.BatchId)).ToList();

                    return new MyClassItem
                    {
                        ClassName = e.Class.ClassName,
                        StudentName = e.Student.FullName,
                        SubjectsSummary = string.Join(", ", e.EnrollmentSubjects.Select(es => es.Subject.SubjectName)),
                        Status = e.Status,
                        EnrolledDateLabel = e.EnrolledDate.HasValue ? e.EnrolledDate.Value.ToString("MMM d, yyyy") : "-",
                        Amount = e.ExpectedAmount,
                        SessionsCompleted = sessionsForThis.Count(s => s.Status == "Completed"),
                        SessionsTotal = sessionsForThis.Count
                    };
                }).ToList()
            };

            return View(model);
        }

        // ============================================================
        // Enroll: Subject picker
        // ============================================================

        private static readonly Dictionary<string, string> ClassSubtitles = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Class 5"] = "Foundational Knowledge",
            ["Class 6"] = "Middle School Entry",
            ["Class 7"] = "Middle School Exploration",
            ["Class 8"] = "Advanced Foundations",
            ["Class 9"] = "High School Prep",
            ["Class 10"] = "Board Exam Preparation",
            ["Class 11"] = "Specialization Track",
            ["Class 12"] = "Senior Secondary Level",
            ["Bachelor"] = "Undergraduate Courses"
        };

        private class SubjectMetaInfo
        {
            public double Rating;
            public string Description = "";
            public string? Badge;
            public string BadgeStyle = "recommended";
            public string IconText = "";
        }

        private static readonly Dictionary<string, SubjectMetaInfo> SubjectMeta = new(StringComparer.OrdinalIgnoreCase)
        {
            ["Mathematics"] = new() { Rating = 4.9, Description = "Master calculus, algebra, and statistics with structured problem-solving sessions.", Badge = "Recommended", BadgeStyle = "recommended", IconText = "\u03A3" },
            ["Science"] = new() { Rating = 4.8, Description = "Explore the laws of physics and chemical reactions through virtual lab experiments.", IconText = "Sc" },
            ["English"] = new() { Rating = 4.7, Description = "Deep dive into classic literature and modern linguistic analysis techniques.", IconText = "En" },
            ["Social"] = new() { Rating = 4.9, Description = "Introduction to programming logic, digital literacy, and modern computing systems.", Badge = "Future Ready", BadgeStyle = "future", IconText = "S" },
            ["Social Studies"] = new() { Rating = 4.5, Description = "Understand global histories, geography, and civic duties in the 21st century.", IconText = "SS" },
            ["Nepali"] = new() { Rating = 4.6, Description = "Comprehensive study of Nepali literature, grammar, and cultural heritage.", IconText = "Ne" },
            ["Computer Science"] = new() { Rating = 4.9, Description = "Introduction to programming logic, digital literacy, and modern computing systems.", Badge = "Future Ready", BadgeStyle = "future", IconText = "CS" },
            ["Art"] = new() { Rating = 4.6, Description = "Explore the unique exploration of art and life.", IconText = "Ar" },
            ["Health and Physical"] = new() { Rating = 4.5, Description = "Promoting physical wellness and healthy lifestyle habits.", IconText = "HP" },
        };

        private static readonly SubjectMetaInfo DefaultSubjectMeta = new()
        {
            Rating = 4.5,
            Description = "Explore this subject with an experienced teacher.",
            IconText = "?"
        };
        [HttpGet]
        public IActionResult Subject(int classId = 0)
        {
            var resolvedClassId = ResolveClassId(classId);
            if (resolvedClassId == null)
            {
                // No class chosen yet - show the class picker grid instead of redirecting away.
                var classes = _context.Classes
                    .Where(c => c.IsActive)
                    .OrderBy(c => c.ClassName)
                    .ToList();

                var studentId = GetCurrentUserId();

                var myEnrollmentsByClass = studentId == null
                    ? new Dictionary<int, string>()
                    : _context.Enrollments
                        .Where(e => e.StudentId == studentId)
                        .GroupBy(e => e.ClassId)
                        .ToDictionary(g => g.Key, g => g.First().Status);

                var enrolledCountsByClass = _context.Enrollments
                    .Where(e => e.Status == "Approved")
                    .GroupBy(e => e.ClassId)
                    .ToDictionary(g => g.Key, g => g.Select(e => e.StudentId).Distinct().Count());

                var pickerModel = classes.Select(c =>
                {
                    var digits = System.Text.RegularExpressions.Regex.Match(c.ClassName, @"\d+").Value;
                    var label = digits != "" ? digits : c.ClassName;
                    var subtitle = ClassSubtitles.TryGetValue(c.ClassName, out var s) ? s : "Explore available subjects";
                    var hasMyStatus = myEnrollmentsByClass.TryGetValue(c.ClassId, out var myStatus);

                    return new ClassPickerItem
                    {
                        ClassId = c.ClassId,
                        ClassName = c.ClassName,
                        Label = label,
                        Subtitle = subtitle,
                        IsEnrolled = hasMyStatus,
                        EnrollmentStatusLabel = hasMyStatus ? myStatus : null,
                        EnrolledStudentCount = enrolledCountsByClass.TryGetValue(c.ClassId, out var count) ? count : 0
                    };
                }).ToList();

                return View("SubjectClassPicker", pickerModel);
            }
            classId = resolvedClassId.Value;

            var classEntity = _context.Classes.FirstOrDefault(c => c.ClassId == classId);
            if (classEntity == null)
            {
                TempData["EnrollError"] = "That class could not be found. Please select a class again.";
                return View("SubjectClassPicker", new List<ClassPickerItem>());
            }

            var subjects = _context.Subjects
                .Where(s => s.ClassId == classId && s.IsActive)
                .Include(s => s.Batches.Where(b => b.IsActive)).ThenInclude(b => b.Teacher)
                .ToList();

            var batchIds = subjects.SelectMany(s => s.Batches).Select(b => b.BatchId).ToList();

            var enrolledCounts = _context.EnrollmentSubjects
                .Where(es => es.AssignedBatchId != null && batchIds.Contains(es.AssignedBatchId.Value))
                .GroupBy(es => es.AssignedBatchId!.Value)
                .ToDictionary(g => g.Key, g => g.Count());

            var selectedSubjectIds = GetSelectedSubjectIds(classId);

            var model = new SubjectPickerViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                Subjects = subjects.Select(s =>
                {
                    var meta = SubjectMeta.TryGetValue(s.SubjectName, out var m) ? m : DefaultSubjectMeta;
                    var primaryBatch = s.Batches.OrderBy(b => b.BatchId).FirstOrDefault();
                    var seatsLeft = primaryBatch == null
                        ? 0
                        : Math.Max(primaryBatch.Capacity - (enrolledCounts.TryGetValue(primaryBatch.BatchId, out var c) ? c : 0), 0);

                    return new SubjectPickerItem
                    {
                        SubjectId = s.SubjectId,
                        Name = s.SubjectName,
                        IconText = meta.IconText,
                        Rating = meta.Rating,
                        Description = meta.Description,
                        Badge = meta.Badge,
                        BadgeStyle = meta.BadgeStyle,
                        TeacherName = primaryBatch?.Teacher.FullName ?? "TBA",
                        SeatsLeft = seatsLeft,
                        IsSelected = selectedSubjectIds.Contains(s.SubjectId)
                    };
                }).ToList()
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult Subject(int classId, List<int>? selectedSubjectIds)
        {
            if (selectedSubjectIds == null || selectedSubjectIds.Count == 0)
            {
                TempData["EnrollError"] = "Please select at least one subject to continue.";
                return RedirectToAction("Subject", new { classId });
            }

            HttpContext.Session.SetString($"EnrollSubjects_{classId}", string.Join(",", selectedSubjectIds));
            return RedirectToAction("Intake", new { classId });
        }



        // ============================================================
        // Enroll: Intake
        // ============================================================

        [HttpGet]
        public IActionResult Intake(int classId = 0)
        {
            try
            {
                var resolvedClassId = ResolveClassId(classId);
                if (resolvedClassId == null)
                {
                    TempData["EnrollError"] = "Please select a class first.";
                    return RedirectToAction("EnrollClass");
                }

                classId = resolvedClassId.Value;

                var selectedSubjectIds = GetSelectedSubjectIds(classId);
                if (selectedSubjectIds.Count == 0)
                {
                    return RedirectToAction("Subject", new { classId });
                }

                var classEntity = _context.Classes.FirstOrDefault(c => c.ClassId == classId);
                if (classEntity == null)
                    return NotFound();

                var courseTypes = _context.CourseTypes.ToList();
                var timeSlots = _context.TimeSlots.ToList();

                var subjects = _context.Subjects
                    .Where(s => selectedSubjectIds.Contains(s.SubjectId))
                    .Include(s => s.Batches.Where(b => b.IsActive))
                        .ThenInclude(b => b.Teacher)
                    .Include(s => s.Batches.Where(b => b.IsActive))
                        .ThenInclude(b => b.TimeSlot)
                    .ToList();

                var batchIds = subjects
                    .SelectMany(s => s.Batches)
                    .Select(b => b.BatchId)
                    .ToList();

                var enrolledCounts = _context.EnrollmentSubjects
                    .Where(es => es.AssignedBatchId != null &&
                                 batchIds.Contains(es.AssignedBatchId.Value))
                    .GroupBy(es => es.AssignedBatchId!.Value)
                    .ToDictionary(g => g.Key, g => g.Count());

                var (savedCourseTypeId, savedTimeSlotId, savedBatches) = GetIntakeSelections(classId);

                // Prevent null dictionary
                savedBatches ??= new Dictionary<int, int>();

                var model = new IntakeViewModel
                {
                    ClassId = classId,
                    ClassName = classEntity.ClassName,

                    CourseTypes = courseTypes.Select(ct => new CourseTypeOption
                    {
                        CourseTypeId = ct.CourseTypeId,
                        TypeName = ct.TypeName
                    }).ToList(),

                    TimeSlots = timeSlots.Select(t => new TimeSlotOption
                    {
                        TimeSlotId = t.TimeSlotId,
                        Label = $"{t.Days}, {t.StartTime:h:mm tt} - {t.EndTime:h:mm tt}"
                    }).ToList(),

                    SelectedCourseTypeId = savedCourseTypeId,
                    SelectedTimeSlotId = savedTimeSlotId,

                    IntakeMonths = NepaliIntakeMonths,
                    SelectedIntakeMonth = HttpContext.Session.GetString($"EnrollIntakeMonth_{classId}"),

                    Subjects = subjects.Select(s => new IntakeSubjectViewModel
                    {
                        SubjectId = s.SubjectId,
                        Name = s.SubjectName,

                        SelectedBatchId = savedBatches.TryGetValue(s.SubjectId, out var bId)
                            ? bId
                            : null,

                        Batches = s.Batches.Select(b => new IntakeBatchOption
                        {
                            BatchId = b.BatchId,
                            CourseTypeId = b.CourseTypeId,

                            // NULL SAFE
                            TeacherName = b.Teacher?.FullName ?? "TBA",

                            // NULL SAFE
                            TimeLabel = b.TimeSlot == null
                                ? "Not Assigned"
                                : $"{b.TimeSlot.Days}, {b.TimeSlot.StartTime:h:mm tt} - {b.TimeSlot.EndTime:h:mm tt}",

                            SeatsLeft = Math.Max(
                                b.Capacity -
                                (enrolledCounts.TryGetValue(b.BatchId, out var c) ? c : 0),
                                0)
                        }).ToList()
                    }).ToList()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                return Content(ex.ToString());
            }
        }

        // ============================================================
        // Enroll: Intake POST
        // ============================================================

        [HttpPost]
        [ActionName("Intake")]
        public IActionResult IntakeSave(int classId, int courseTypeId, int preferredTimeSlotId,
            string? intakeMonth, Dictionary<int, int>? selectedBatches)
        {
            if (courseTypeId == 0 || preferredTimeSlotId == 0)
            {
                TempData["EnrollError"] = "Please select a plan and time slot.";
                return RedirectToAction("Intake", new { classId });
            }

            HttpContext.Session.SetInt32($"EnrollCourseType_{classId}", courseTypeId);
            HttpContext.Session.SetInt32($"EnrollTimeSlot_{classId}", preferredTimeSlotId);

            if (!string.IsNullOrEmpty(intakeMonth))
                HttpContext.Session.SetString($"EnrollIntakeMonth_{classId}", intakeMonth);

            if (selectedBatches != null && selectedBatches.Count > 0)
            {
                var json = JsonSerializer.Serialize(selectedBatches);
                HttpContext.Session.SetString($"EnrollBatches_{classId}", json);
            }

            return RedirectToAction("Pricing", new { classId });
        }

        // ============================================================
        // Enroll: Pricing
        // ============================================================

        [HttpGet]
        public IActionResult Pricing(int classId = 0)
        {
            var resolvedClassId = ResolveClassId(classId);
            if (resolvedClassId == null)
            {
                TempData["EnrollError"] = "Please select a class first.";
                return RedirectToAction("EnrollClass");
            }
            classId = resolvedClassId.Value;

            var selectedSubjectIds = GetSelectedSubjectIds(classId);
            if (selectedSubjectIds.Count == 0)
                return RedirectToAction("Subject", new { classId });

            var (courseTypeId, timeSlotId, batches) = GetIntakeSelections(classId);
            if (courseTypeId == null || timeSlotId == null || selectedSubjectIds.Any(id => !batches.ContainsKey(id)))
                return RedirectToAction("Intake", new { classId });

            var classEntity = _context.Classes.FirstOrDefault(c => c.ClassId == classId);
            if (classEntity == null) return NotFound();

            var courseType = _context.CourseTypes.FirstOrDefault(ct => ct.CourseTypeId == courseTypeId);

            var subjects = _context.Subjects
                .Where(s => selectedSubjectIds.Contains(s.SubjectId))
                .ToList();

            var fees = _context.CourseFees
                .Where(f => f.ClassId == classId
                         && f.CourseTypeId == courseTypeId
                         && selectedSubjectIds.Contains(f.SubjectId)
                         && f.IsActive)
                .ToList();

            var items = subjects.Select(s =>
            {
                var fee = fees.FirstOrDefault(f => f.SubjectId == s.SubjectId);
                return new PricingLineItem
                {
                    SubjectName = s.SubjectName,
                    Amount = fee?.Amount
                };
            }).ToList();

            var model = new PricingViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                CourseTypeName = courseType?.TypeName ?? "",
                Items = items,
                Total = items.Sum(i => i.Amount ?? 0),
                HasMissingFees = items.Any(i => i.Amount == null)
            };

            return View(model);
        }

        [HttpPost]
        [ActionName("Pricing")]
        public IActionResult PricingConfirm(int classId)
        {
            var selectedSubjectIds = GetSelectedSubjectIds(classId);
            var (courseTypeId, timeSlotId, batches) = GetIntakeSelections(classId);

            if (courseTypeId == null || timeSlotId == null || selectedSubjectIds.Any(id => !batches.ContainsKey(id)))
            {
                TempData["EnrollError"] = "Please complete the intake step first.";
                return RedirectToAction("Intake", new { classId });
            }

            var fees = _context.CourseFees
                .Where(f => f.ClassId == classId
                         && f.CourseTypeId == courseTypeId
                         && selectedSubjectIds.Contains(f.SubjectId)
                         && f.IsActive)
                .ToList();

            if (fees.Count < selectedSubjectIds.Count)
            {
                TempData["EnrollError"] = "Pricing isn't available yet for one or more selected subjects. Please contact the office.";
                return RedirectToAction("Pricing", new { classId });
            }

            var total = fees.Sum(f => f.Amount);
            HttpContext.Session.SetString($"EnrollTotal_{classId}", total.ToString(System.Globalization.CultureInfo.InvariantCulture));

            return RedirectToAction("Payments", new { classId });
        }

        // ============================================================
        // Enroll: Payments (GET) — Summary + payment form
        // ============================================================

        [HttpGet]
        public IActionResult Payments(int classId = 0)
        {
            var resolvedClassId = ResolveClassId(classId);
            if (resolvedClassId == null)
            {
                TempData["EnrollError"] = "Please select a class first.";
                return RedirectToAction("EnrollClass");
            }
            classId = resolvedClassId.Value;

            var selectedSubjectIds = GetSelectedSubjectIds(classId);
            if (selectedSubjectIds.Count == 0)
                return RedirectToAction("Subject", new { classId });

            var (courseTypeId, timeSlotId, batches) = GetIntakeSelections(classId);
            if (courseTypeId == null || timeSlotId == null)
                return RedirectToAction("Intake", new { classId });

            var classEntity = _context.Classes.FirstOrDefault(c => c.ClassId == classId);
            if (classEntity == null) return NotFound();

            var courseType = _context.CourseTypes.FirstOrDefault(ct => ct.CourseTypeId == courseTypeId);

            var subjects = _context.Subjects
                .Where(s => selectedSubjectIds.Contains(s.SubjectId))
                .ToList();

            var totalStr = HttpContext.Session.GetString($"EnrollTotal_{classId}");
            decimal total = 0;
            if (!string.IsNullOrEmpty(totalStr))
                decimal.TryParse(totalStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out total);

            if (total == 0)
            {
                var fees = _context.CourseFees
                    .Where(f => f.ClassId == classId && f.CourseTypeId == courseTypeId && selectedSubjectIds.Contains(f.SubjectId) && f.IsActive)
                    .ToList();
                total = fees.Sum(f => f.Amount);
            }

            var model = new PaymentsViewModel
            {
                ClassId = classId,
                ClassName = classEntity.ClassName,
                CourseTypeName = courseType?.TypeName ?? "",
                SubjectsSummary = string.Join(", ", subjects.Select(s => s.SubjectName)),
                Total = total
            };

            return View(model);
        }

        // ============================================================
        // Enroll: Payments (POST) — Save enrollment to DB
        // ============================================================

        [HttpPost]
        [ActionName("Payments")]
        public async Task<IActionResult> PaymentsSubmit(int classId, string paymentMethod,
            string? transactionId, IFormFile? screenshotFile)
        {
            var studentId = GetCurrentUserId();
            if (studentId == null) return RedirectToAction("Login", "Account");

            var selectedSubjectIds = GetSelectedSubjectIds(classId);
            if (selectedSubjectIds.Count == 0)
                return RedirectToAction("Subject", new { classId });

            var (courseTypeId, timeSlotId, batches) = GetIntakeSelections(classId);
            if (courseTypeId == null || timeSlotId == null)
                return RedirectToAction("Intake", new { classId });

            // Guard: prevent duplicate active enrollment in same class
            var duplicate = _context.Enrollments.FirstOrDefault(e =>
                e.StudentId == studentId && e.ClassId == classId &&
                (e.Status == "Pending" || e.Status == "Approved"));
            if (duplicate != null)
            {
                TempData["EnrollError"] = "You already have an active or pending enrollment for this class.";
                return RedirectToAction("EnrollClass");
            }

            // Resolve total
            var totalStr = HttpContext.Session.GetString($"EnrollTotal_{classId}");
            decimal total = 0;
            if (!string.IsNullOrEmpty(totalStr))
                decimal.TryParse(totalStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out total);

            if (total == 0)
            {
                var fees = _context.CourseFees
                    .Where(f => f.ClassId == classId && f.CourseTypeId == courseTypeId &&
                                selectedSubjectIds.Contains(f.SubjectId) && f.IsActive)
                    .ToList();
                total = fees.Sum(f => f.Amount);
            }

            // Generate enrollment number
            var enrollmentNumber = $"ENR-{DateTime.Now:yyyyMMdd}-{new Random().Next(1000, 9999)}";

            var enrollment = new Enrollment
            {
                EnrollmentNumber = enrollmentNumber,
                StudentId = studentId.Value,
                ClassId = classId,
                CourseTypeId = courseTypeId.Value,
                PreferredTimeSlotId = timeSlotId.Value,
                ExpectedAmount = total,
                Status = "Pending",
                EnrolledDate = DateTime.Now
            };

            _context.Enrollments.Add(enrollment);
            await _context.SaveChangesAsync();

            // Add enrollment subjects
            foreach (var subjectId in selectedSubjectIds)
            {
                batches.TryGetValue(subjectId, out var batchId);
                _context.EnrollmentSubjects.Add(new EnrollmentSubject
                {
                    EnrollmentId = enrollment.EnrollmentId,
                    SubjectId = subjectId,
                    AssignedBatchId = batchId > 0 ? batchId : null
                });
            }

            // Handle screenshot upload
            string? screenshotPath = null;
            if (screenshotFile != null && screenshotFile.Length > 0)
            {
                var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", "payments");
                Directory.CreateDirectory(uploadsFolder);
                var ext = Path.GetExtension(screenshotFile.FileName).ToLower();
                if (string.IsNullOrEmpty(ext)) ext = ".jpg";
                var fileName = $"pay_{enrollment.EnrollmentId}_{DateTime.Now:yyyyMMddHHmmss}{ext}";
                var filePath = Path.Combine(uploadsFolder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await screenshotFile.CopyToAsync(stream);
                screenshotPath = $"/uploads/payments/{fileName}";
            }

            // Create payment record
            _context.Payments.Add(new Payment
            {
                EnrollmentId = enrollment.EnrollmentId,
                Amount = total,
                Method = string.IsNullOrWhiteSpace(paymentMethod) ? "Unknown" : paymentMethod,
                TransactionId = transactionId,
                ScreenshotPath = screenshotPath,
                Status = "Pending",
                PaymentDate = DateTime.Now
            });

            await _context.SaveChangesAsync();

            // Clear session for this enrollment flow
            var classKey = classId.ToString();
            HttpContext.Session.Remove($"EnrollSubjects_{classKey}");
            HttpContext.Session.Remove($"EnrollCourseType_{classKey}");
            HttpContext.Session.Remove($"EnrollTimeSlot_{classKey}");
            HttpContext.Session.Remove($"EnrollBatches_{classKey}");
            HttpContext.Session.Remove($"EnrollIntakeMonth_{classKey}");
            HttpContext.Session.Remove($"EnrollTotal_{classKey}");

            // Fetch class name for confirmation
            var classEntity = _context.Classes.FirstOrDefault(c => c.ClassId == classId);

            TempData["ConfirmEnrollmentNumber"] = enrollmentNumber;
            TempData["ConfirmClassName"] = classEntity?.ClassName ?? "";
            TempData["ConfirmTotal"] = total.ToString("N0");

            return RedirectToAction("Confirmation");
        }

        // ============================================================
        // Enroll: Confirmation
        // ============================================================

        [HttpGet]
        public IActionResult Confirmation()
        {
            var enrollmentNumber = TempData["ConfirmEnrollmentNumber"] as string;
            if (string.IsNullOrEmpty(enrollmentNumber))
                return RedirectToAction("Dashboard");

            var model = new EnrollmentConfirmationViewModel
            {
                EnrollmentNumber = enrollmentNumber,
                ClassName = TempData["ConfirmClassName"] as string ?? "",
                Total = TempData["ConfirmTotal"] as string ?? "0"
            };

            return View(model);
        }


        // ============================================================
        // Private helpers
        // ============================================================

        private int? ResolveClassId(int classId)
        {
            if (classId > 0)
            {
                HttpContext.Session.SetInt32(CurrentClassSessionKey, classId);
                return classId;
            }

            return HttpContext.Session.GetInt32(CurrentClassSessionKey);
        }

        private List<int> GetSelectedSubjectIds(int classId)
        {
            var raw = HttpContext.Session.GetString($"EnrollSubjects_{classId}");
            return string.IsNullOrEmpty(raw)
                ? new List<int>()
                : raw.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToList();
        }

        private (int? courseTypeId, int? timeSlotId, Dictionary<int, int> batches) GetIntakeSelections(int classId)
        {
            var courseTypeId = HttpContext.Session.GetInt32($"EnrollCourseType_{classId}");
            var timeSlotId = HttpContext.Session.GetInt32($"EnrollTimeSlot_{classId}");
            var raw = HttpContext.Session.GetString($"EnrollBatches_{classId}");
            var batches = string.IsNullOrEmpty(raw)
                ? new Dictionary<int, int>()
                : JsonSerializer.Deserialize<Dictionary<int, int>>(raw) ?? new();

            return (courseTypeId, timeSlotId, batches);
        }

        [HttpGet]
        public IActionResult Profile()
        {
            var userId = GetCurrentUserId();
            var student = _context.Users
                .Include(u => u.StudentProfile)
                .FirstOrDefault(u => u.UserId == userId);

            if (student == null)
            {
                return RedirectToAction("Login", "Account");
            }

            return View(student);
        }

        [HttpPost]
        public async Task<IActionResult> Profile(string fullName, string phone, IFormFile? photoFile)
        {
            var userId = GetCurrentUserId();
            var student = _context.Users.FirstOrDefault(u => u.UserId == userId);

            if (student != null)
            {
                if (!string.IsNullOrWhiteSpace(fullName))
                {
                    student.FullName = fullName;
                }
                student.Phone = phone ?? student.Phone;

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

                    var filePath = Path.Combine(uploadsFolder, $"user_{student.UserId}{extension}");
                    using (var stream = new FileStream(filePath, FileMode.Create))
                    {
                        await photoFile.CopyToAsync(stream);
                    }

                    student.ProfileImage = $"/uploads/profiles/user_{student.UserId}{extension}";
                }

                _context.SaveChanges();

                // Refresh authentication claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, student.FullName),
                    new Claim(ClaimTypes.NameIdentifier, student.UserId.ToString()),
                    new Claim(ClaimTypes.Role, student.Role),
                    new Claim("Email", student.Email),
                    new Claim("Phone", student.Phone ?? "")
                };
                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity));

                TempData["SuccessMessage"] = "Your profile details have been successfully updated!";
            }

            return RedirectToAction("Profile");
        }

        private int? GetCurrentUserId()
        {
            var claim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
            return claim != null && int.TryParse(claim.Value, out var id) ? id : null;
        }

        private static string GetStartsInLabel(DateOnly sessionDate, TimeOnly startTime)
        {
            var sessionDateTime = sessionDate.ToDateTime(startTime);
            var diff = sessionDateTime - DateTime.Now;

            if (diff.TotalMinutes < 0) return "In progress";
            if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes} min";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h {diff.Minutes} min";
            return sessionDateTime.ToString("MMM d, h:mm tt");
        }

        private static string GetTimeAgo(DateTime? createdDate)
        {
            if (createdDate == null) return "";
            var diff = DateTime.Now - createdDate.Value;

            if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes} minutes ago";
            if (diff.TotalHours < 24) return $"{(int)diff.TotalHours} hours ago";
            if (diff.TotalDays < 2) return "Yesterday";
            return createdDate.Value.ToString("MMM d, yyyy");
        }

        private static readonly List<IntakeMonthOption> NepaliIntakeMonths = BuildIntakeMonths();

        private static List<IntakeMonthOption> BuildIntakeMonths()
        {
            var months = new (string Name, string Year, DateTime Start)[]
            {
                ("Baisakh", "2081", new DateTime(2024, 4, 14)),
                ("Jestha",  "2081", new DateTime(2024, 5, 15)),
                ("Ashad",   "2081", new DateTime(2024, 6, 15)),
                ("Shrawan", "2081", new DateTime(2024, 7, 16)),
                ("Bhadra",  "2081", new DateTime(2024, 8, 17)),
                ("Ashwin",  "2081", new DateTime(2024, 9, 16)),
                ("Kartik",  "2081", new DateTime(2024, 10, 17)),
                ("Mangsir", "2081", new DateTime(2024, 11, 16)),
                ("Poush",   "2081", new DateTime(2024, 12, 15)),
                ("Magh",    "2081", new DateTime(2025, 1, 14)),
                ("Falgun",  "2081", new DateTime(2025, 2, 12)),
                ("Chaitra", "2081", new DateTime(2025, 3, 14)),
            };

            return months.Select((m, i) => new IntakeMonthOption
            {
                Value = $"{m.Name}-{m.Year}",
                MonthName = m.Name,
                Year = m.Year,
                StartDate = m.Start,
                StartDateLabel = m.Start.ToString("MMMM d, yyyy"),
                DurationMonths = 6,
                IsPopular = i == 0
            }).ToList();
        }
    }
}