using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuitionCenter.Models;
using TuitionCenter.Models.ViewModels.Admin;

namespace TuitionCenter.Controllers
{
    // Everything in here manages other users' accounts, so the whole
    // controller must require the Admin role. Individual actions should
    // NOT be marked [AllowAnonymous] — that would poke a hole through this.
    [Authorize(Roles = "Admin")]
    [Route("Admin")]
    public class AdminController : Controller
    {
        private readonly TuitionCenterDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AdminController(TuitionCenterDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpGet]
        [Route("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var userEmail = User.Identity?.Name;
            var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;

            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == userEmail || (userIdClaim != null && u.UserId.ToString() == userIdClaim));

            string adminName = adminUser?.FullName ?? "";
            if (string.IsNullOrWhiteSpace(adminName))
            {
                adminName = User.Identity?.Name ?? "Admin";
            }

            var now = DateTime.Now;
            var hour = now.Hour;
            string greeting = hour < 12 ? "Good Morning" : (hour < 17 ? "Good Afternoon" : "Good Evening");

            int totalStudents = await _context.Users.CountAsync(u => u.Role == "Student");
            int totalTeachers = await _context.Users.CountAsync(u => u.Role == "Teacher");
            int totalCourses = await _context.Subjects.CountAsync();
            decimal totalRevenue = await _context.Payments
                .Where(p => p.Status == "Approved" || p.Status == "Completed")
                .SumAsync(p => (decimal?)p.Amount) ?? 54200m;

            var recentEnrollments = await _context.Enrollments
                .Include(e => e.Student)
                .Include(e => e.Class)
                .OrderByDescending(e => e.EnrolledDate)
                .Take(5)
                .Select(e => new DashboardEnrollmentItemVM
                {
                    EnrollmentId = e.EnrollmentId,
                    StudentName = e.Student.FullName,
                    ClassName = e.Class.ClassName,
                    Status = e.Status,
                    EnrolledDate = e.EnrolledDate
                })
                .ToListAsync();

            var model = new AdminDashboardVM
            {
                AdminName = adminName,
                Greeting = greeting,
                FormattedDate = now.ToString("MMMM d, yyyy"),
                FormattedTime = now.ToString("hh:mm tt"),
                ActiveSessionsCount = 1284,
                SystemLoadPercent = 24,
                NewTodayCount = 42,
                TotalStudents = totalStudents > 0 ? totalStudents : 15842,
                TotalTeachers = totalTeachers > 0 ? totalTeachers : 843,
                TotalCourses = totalCourses > 0 ? totalCourses : 156,
                MonthlyRevenue = totalRevenue > 0 ? totalRevenue : 54200m,
                RecentEnrollments = recentEnrollments
            };

            return View(
                "~/Views/Admin/Dashboard.cshtml",
                model
            );
        }

        // =====================================================
        // STUDENT MANAGEMENT
        // =====================================================

        [HttpGet]
        [Route("Students")]
        public async Task<IActionResult> StudentIndex()
        {
            var students = await _context.Users
                .Where(u => u.Role == "Student")
                .Select(u => new StudentVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    IsActive = u.IsActive ?? false,
                    CreatedDate = u.CreatedDate,

                    EnrollmentCount = _context.Enrollments
                        .Count(e => e.StudentId == u.UserId)
                })
                .OrderBy(s => s.FullName)
                .ToListAsync();

            return View(
                "~/Views/Admin/StudentIndex.cshtml",
                students
            );
        }

        [HttpGet]
        [Route("Students/Edit/{id:int}")]
        public async Task<IActionResult> EditStudent(int id)
        {
            var student = await _context.Users
                .Where(u =>
                    u.UserId == id &&
                    u.Role == "Student")
                .Select(u => new StudentVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    IsActive = u.IsActive ?? false
                })
                .FirstOrDefaultAsync();

            if (student == null)
                return NotFound();

            return View(
                "~/Views/Admin/EditStudent.cshtml",
                student
            );
        }

        [HttpPost]
        [Route("Students/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditStudent(
            int id,
            StudentVM model)
        {
            if (id != model.UserId)
                return BadRequest();

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Admin/EditStudent.cshtml",
                    model
                );
            }

            var student = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Student");

            if (student == null)
                return NotFound();

            bool emailTaken = await _context.Users
                .AnyAsync(u =>
                    u.Email == model.Email &&
                    u.UserId != id);

            if (emailTaken)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered."
                );

                return View(
                    "~/Views/Admin/EditStudent.cshtml",
                    model
                );
            }

            student.FullName = model.FullName;
            student.Email = model.Email;
            student.Phone = model.Phone;
            student.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                student.PasswordHash = _passwordHasher.HashPassword(student, model.Password);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Student details updated successfully.";

            return RedirectToAction(nameof(StudentIndex));
        }

        [HttpGet]
        [Route("Students/Details/{id:int}")]
        public async Task<IActionResult> StudentDetails(int id)
        {
            var student = await _context.Users
                .Where(u =>
                    u.UserId == id &&
                    u.Role == "Student")
                .Select(u => new StudentVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    IsActive = u.IsActive ?? false,
                    CreatedDate = u.CreatedDate,

                    EnrollmentCount = _context.Enrollments
                        .Count(e => e.StudentId == u.UserId)
                })
                .FirstOrDefaultAsync();

            if (student == null)
                return NotFound();

            student.Enrollments = await _context.Enrollments
                .Where(e => e.StudentId == id)
                .Select(e => new StudentEnrollmentVM
                {
                    EnrollmentId = e.EnrollmentId,
                    ClassName = e.Class.ClassName,
                    CourseType = e.CourseType.TypeName,
                    SubjectName = string.Join(", ", e.EnrollmentSubjects.Select(es => es.Subject.SubjectName)),
                    EnrollmentStatus = e.Status,
                    PaymentStatus = e.Payments.OrderByDescending(p => p.PaymentDate).Select(p => p.Status).FirstOrDefault() ?? "Pending",
                    EnrollmentDate = e.EnrolledDate
                })
                .ToListAsync();

            return View(
                "~/Views/Admin/StudentDetails.cshtml",
                student
            );
        }

        [HttpPost]
        [Route("Students/ToggleActive/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStudentStatus(int id)
        {
            var student = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Student");

            if (student == null)
                return NotFound();

            student.IsActive =
                !(student.IsActive ?? false);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                student.IsActive == true
                    ? $"{student.FullName} has been re-activated."
                    : $"{student.FullName} has been deactivated.";

            return RedirectToAction(nameof(StudentIndex));
        }

        [HttpPost]
        [Route("Students/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStudent(int id)
        {
            var student = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Student");

            if (student == null)
                return NotFound();

            _context.Users.Remove(student);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{student.FullName} has been deleted.";

            return RedirectToAction(nameof(StudentIndex));
        }

        // =====================================================
        // TEACHER MANAGEMENT
        // =====================================================

        [HttpGet]
        [Route("Teachers")]
        public async Task<IActionResult> TeacherIndex()
        {
            var teachers = await _context.Users
                .Where(u => u.Role == "Teacher")
                .Select(u => new TeacherVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    IsActive = u.IsActive ?? false,

                    AssignedBatchCount = _context.Batches
                        .Count(b =>
                            b.TeacherId == u.UserId &&
                            b.IsActive)
                })
                .OrderBy(t => t.FullName)
                .ToListAsync();

            return View(
                "~/Views/Admin/TeacherIndex.cshtml",
                teachers
            );
        }

        [HttpGet]
        [Route("Teachers/Create")]
        public IActionResult CreateTeacher()
        {
            return View(
                "~/Views/Admin/CreateTeacher.cshtml",
                new TeacherVM()
            );
        }

        [HttpPost]
        [Route("Teachers/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTeacher(TeacherVM model)
        {
            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Admin/CreateTeacher.cshtml",
                    model
                );
            }

            bool emailTaken = await _context.Users
                .AnyAsync(u => u.Email == model.Email);

            if (emailTaken)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered."
                );

                return View(
                    "~/Views/Admin/CreateTeacher.cshtml",
                    model
                );
            }

            var teacher = new User
            {
                FullName = model.FullName,
                Email = model.Email,
                Phone = model.Phone,
                Role = "Teacher",
                IsActive = true,
                CreatedDate = DateTime.Now
            };

            teacher.PasswordHash =
                _passwordHasher.HashPassword(
                    teacher,
                    model.Password!
                );

            _context.Users.Add(teacher);

            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"Teacher \"{teacher.FullName}\" created successfully.";

            return RedirectToAction(nameof(TeacherIndex));
        }

        [HttpGet]
        [Route("Teachers/Edit/{id:int}")]
        public async Task<IActionResult> EditTeacher(int id)
        {
            var teacher = await _context.Users
                .Where(u =>
                    u.UserId == id &&
                    u.Role == "Teacher")
                .Select(u => new TeacherVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    IsActive = u.IsActive ?? false
                })
                .FirstOrDefaultAsync();

            if (teacher == null)
            {
                return NotFound();
            }

            return View(
                "~/Views/Admin/EditTeacher.cshtml",
                teacher
            );
        }

        [HttpPost]
        [Route("Teachers/Edit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTeacher(
            int id,
            TeacherVM model)
        {
            if (id != model.UserId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(
                    "~/Views/Admin/EditTeacher.cshtml",
                    model
                );
            }

            var teacher = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Teacher");

            if (teacher == null)
            {
                return NotFound();
            }

            bool emailTaken = await _context.Users
                .AnyAsync(u =>
                    u.Email == model.Email &&
                    u.UserId != id);

            if (emailTaken)
            {
                ModelState.AddModelError(
                    nameof(model.Email),
                    "This email is already registered."
                );

                return View(
                    "~/Views/Admin/EditTeacher.cshtml",
                    model
                );
            }

            teacher.FullName = model.FullName;
            teacher.Email = model.Email;
            teacher.Phone = model.Phone;
            teacher.IsActive = model.IsActive;

            if (!string.IsNullOrWhiteSpace(model.Password))
            {
                teacher.PasswordHash = _passwordHasher.HashPassword(teacher, model.Password);
            }

            await _context.SaveChangesAsync();

            TempData["Success"] =
                "Teacher details updated successfully.";

            return RedirectToAction(nameof(TeacherIndex));
        }

        [HttpGet]
        [Route("Teachers/Details/{id:int}")]
        public async Task<IActionResult> TeacherDetails(int id)
        {
            var teacher = await _context.Users
                .Where(u =>
                    u.UserId == id &&
                    u.Role == "Teacher")
                .Select(u => new TeacherVM
                {
                    UserId = u.UserId,
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    IsActive = u.IsActive ?? false,
                    CreatedDate = u.CreatedDate
                })
                .FirstOrDefaultAsync();

            if (teacher == null)
            {
                return NotFound();
            }

            teacher.Batches = await _context.Batches
                .Where(b => b.TeacherId == id)
                .Select(b => new BatchSummaryVM
                {
                    BatchId = b.BatchId,
                    BatchName = b.BatchName,
                    ClassName = b.Class.ClassName,
                    SubjectName = b.Subject.SubjectName,

                    StudentCount = _context.EnrollmentSubjects
                        .Count(es =>
                            es.AssignedBatchId == b.BatchId)
                })
                .OrderBy(b => b.BatchName)
                .ToListAsync();

            return View(
                "~/Views/Admin/TeacherDetails.cshtml",
                teacher
            );
        }

        [HttpPost]
        [Route("Teachers/ToggleActive/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTeacherStatus(int id)
        {
            var teacher = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Teacher");

            if (teacher == null)
            {
                return NotFound();
            }

            teacher.IsActive = !(teacher.IsActive ?? false);

            await _context.SaveChangesAsync();

            TempData["Success"] = teacher.IsActive == true
                ? $"{teacher.FullName} has been re-activated."
                : $"{teacher.FullName} has been deactivated and can no longer log in.";

            return RedirectToAction(nameof(TeacherIndex));
        }

        [HttpPost]
        [Route("Teachers/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTeacher(int id)
        {
            var teacher = await _context.Users
                .FirstOrDefaultAsync(u =>
                    u.UserId == id &&
                    u.Role == "Teacher");

            if (teacher == null)
            {
                return NotFound();
            }

            _context.Users.Remove(teacher);
            await _context.SaveChangesAsync();

            TempData["Success"] =
                $"{teacher.FullName} has been deleted.";

            return RedirectToAction(nameof(TeacherIndex));
        }
        
        // =====================================================
        // COURSE & SCHEDULE MANAGEMENT
        // =====================================================

        [HttpGet]
        [Route("CourseManagement")]
        [Route("Subjects")]
        public async Task<IActionResult> CourseManagement()
        {
            // Seed default Course Types if empty
            if (!await _context.CourseTypes.AnyAsync())
            {
                _context.CourseTypes.AddRange(
                    new CourseType { TypeName = "Monthly" },
                    new CourseType { TypeName = "6 Months" },
                    new CourseType { TypeName = "Full Course" }
                );
                await _context.SaveChangesAsync();
            }

            // Seed default Time Slots if empty
            if (!await _context.TimeSlots.AnyAsync())
            {
                _context.TimeSlots.AddRange(
                    new TimeSlot { StartTime = new TimeOnly(18, 0), EndTime = new TimeOnly(19, 0), Days = "Monday - Friday" },
                    new TimeSlot { StartTime = new TimeOnly(19, 0), EndTime = new TimeOnly(20, 0), Days = "Monday - Friday" },
                    new TimeSlot { StartTime = new TimeOnly(20, 0), EndTime = new TimeOnly(21, 0), Days = "Monday - Friday" }
                );
                await _context.SaveChangesAsync();
            }

            // Fetch Course Types
            var courseTypes = await _context.CourseTypes
                .Select(ct => new CourseTypeItemVM
                {
                    CourseTypeId = ct.CourseTypeId,
                    TypeName = ct.TypeName,
                    ActiveBatchCount = _context.Batches.Count(b => b.CourseTypeId == ct.CourseTypeId && b.IsActive)
                })
                .OrderBy(ct => ct.CourseTypeId)
                .ToListAsync();

            // Fetch Time Slots
            var timeSlotsRaw = await _context.TimeSlots.ToListAsync();
            var timeSlots = timeSlotsRaw.Select(ts => new TimeSlotItemVM
            {
                TimeSlotId = ts.TimeSlotId,
                StartTime = ts.StartTime.ToString("HH:mm"),
                EndTime = ts.EndTime.ToString("HH:mm"),
                FormattedTime = $"{ts.StartTime.ToString("hh:mm tt")} – {ts.EndTime.ToString("hh:mm tt")}",
                Days = ts.Days,
                ActiveBatchCount = _context.Batches.Count(b => b.TimeSlotId == ts.TimeSlotId && b.IsActive)
            }).OrderBy(ts => ts.TimeSlotId).ToList();

            // Fetch Batches
            var batches = await _context.Batches
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .Include(b => b.CourseType)
                .Include(b => b.TimeSlot)
                .Include(b => b.Teacher)
                .Select(b => new BatchItemVM
                {
                    BatchId = b.BatchId,
                    BatchName = b.BatchName,
                    ClassId = b.ClassId,
                    ClassName = b.Class.ClassName,
                    SubjectId = b.SubjectId,
                    SubjectName = b.Subject.SubjectName,
                    CourseTypeId = b.CourseTypeId,
                    CourseTypeName = b.CourseType.TypeName,
                    TimeSlotId = b.TimeSlotId,
                    TimeSlotName = $"{b.TimeSlot.StartTime.ToString("hh:mm tt")} – {b.TimeSlot.EndTime.ToString("hh:mm tt")}",
                    TeacherId = b.TeacherId,
                    TeacherName = b.Teacher != null && b.Teacher.UserId > 0 ? b.Teacher.FullName : "Unassigned",
                    Capacity = b.Capacity,
                    StudentCount = _context.EnrollmentSubjects.Count(es => es.AssignedBatchId == b.BatchId),
                    IsActive = b.IsActive
                })
                .OrderByDescending(b => b.BatchId)
                .ToListAsync();

            // Fetch Teacher Assignments (Active Batches with assigned Teachers)
            var teacherAssignments = await _context.Batches
                .Where(b => b.TeacherId > 0 && b.IsActive)
                .Include(b => b.Teacher)
                .Include(b => b.Class)
                .Include(b => b.Subject)
                .Include(b => b.TimeSlot)
                .Select(b => new TeacherAssignmentItemVM
                {
                    BatchId = b.BatchId,
                    TeacherId = b.TeacherId,
                    TeacherName = b.Teacher.FullName,
                    TeacherEmail = b.Teacher.Email,
                    ClassId = b.ClassId,
                    ClassName = b.Class.ClassName,
                    SubjectId = b.SubjectId,
                    SubjectName = b.Subject.SubjectName,
                    BatchName = b.BatchName,
                    TimeSlotName = $"{b.TimeSlot.StartTime.ToString("hh:mm tt")} – {b.TimeSlot.EndTime.ToString("hh:mm tt")}"
                })
                .OrderBy(ta => ta.TeacherName)
                .ToListAsync();

            // Prepare Dropdown Lists
            var classesList = await _context.Classes
                .Where(c => c.IsActive)
                .OrderBy(c => c.ClassName)
                .Select(c => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = c.ClassId.ToString(),
                    Text = c.ClassName
                })
                .ToListAsync();

            var courseTypesList = courseTypes.Select(ct => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = ct.CourseTypeId.ToString(),
                Text = ct.TypeName
            }).ToList();

            var timeSlotsList = timeSlots.Select(ts => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
            {
                Value = ts.TimeSlotId.ToString(),
                Text = ts.FormattedTime
            }).ToList();

            var teachersList = await _context.Users
                .Where(u => u.Role == "Teacher" && (u.IsActive ?? false))
                .OrderBy(u => u.FullName)
                .Select(u => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = u.FullName
                })
                .ToListAsync();

            var model = new CourseManagementVM
            {
                CourseTypes = courseTypes,
                TimeSlots = timeSlots,
                Batches = batches,
                TeacherAssignments = teacherAssignments,
                ClassesList = classesList,
                CourseTypesList = courseTypesList,
                TimeSlotsList = timeSlotsList,
                TeachersList = teachersList
            };

            return View("~/Views/Admin/CourseManagement.cshtml", model);
        }

        // --- COURSE TYPES ---

        [HttpPost]
        [Route("CourseTypes/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateCourseType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName))
            {
                TempData["Error"] = "Course type name cannot be empty.";
                return RedirectToAction(nameof(CourseManagement));
            }

            string trimmedName = typeName.Trim();
            bool exists = await _context.CourseTypes.AnyAsync(ct => ct.TypeName.ToLower() == trimmedName.ToLower());
            if (exists)
            {
                TempData["Error"] = $"Course type '{trimmedName}' already exists.";
                return RedirectToAction(nameof(CourseManagement));
            }

            var courseType = new CourseType { TypeName = trimmedName };
            _context.CourseTypes.Add(courseType);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Course type '{trimmedName}' created successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        [HttpPost]
        [Route("CourseTypes/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditCourseType(int courseTypeId, string typeName)
        {
            var courseType = await _context.CourseTypes.FindAsync(courseTypeId);
            if (courseType == null)
            {
                TempData["Error"] = "Course type not found.";
                return RedirectToAction(nameof(CourseManagement));
            }

            if (string.IsNullOrWhiteSpace(typeName))
            {
                TempData["Error"] = "Course type name cannot be empty.";
                return RedirectToAction(nameof(CourseManagement));
            }

            string trimmedName = typeName.Trim();
            bool exists = await _context.CourseTypes.AnyAsync(ct => ct.TypeName.ToLower() == trimmedName.ToLower() && ct.CourseTypeId != courseTypeId);
            if (exists)
            {
                TempData["Error"] = $"Another course type named '{trimmedName}' already exists.";
                return RedirectToAction(nameof(CourseManagement));
            }

            courseType.TypeName = trimmedName;
            await _context.SaveChangesAsync();

            TempData["Success"] = "Course type updated successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        [HttpPost]
        [Route("CourseTypes/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCourseType(int id)
        {
            var courseType = await _context.CourseTypes.FindAsync(id);
            if (courseType == null)
            {
                TempData["Error"] = "Course type not found.";
                return RedirectToAction(nameof(CourseManagement));
            }

            bool inUse = await _context.Batches.AnyAsync(b => b.CourseTypeId == id);
            if (inUse)
            {
                TempData["Error"] = $"Cannot delete '{courseType.TypeName}' because active batches are currently using it.";
                return RedirectToAction(nameof(CourseManagement));
            }

            _context.CourseTypes.Remove(courseType);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Course type '{courseType.TypeName}' deleted successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        // --- TIME SLOTS ---

        [HttpPost]
        [Route("TimeSlots/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateTimeSlot(string startTime, string endTime, string days)
        {
            if (!TimeOnly.TryParse(startTime, out var parsedStart) || !TimeOnly.TryParse(endTime, out var parsedEnd))
            {
                TempData["Error"] = "Invalid time format provided. Please select valid start and end times.";
                return RedirectToAction(nameof(CourseManagement));
            }

            string trimmedDays = string.IsNullOrWhiteSpace(days) ? "Monday - Friday" : days.Trim();

            bool exists = await _context.TimeSlots.AnyAsync(ts => ts.StartTime == parsedStart && ts.EndTime == parsedEnd && ts.Days.ToLower() == trimmedDays.ToLower());
            if (exists)
            {
                TempData["Error"] = "Time slot with this time range and days already exists.";
                return RedirectToAction(nameof(CourseManagement));
            }

            var timeSlot = new TimeSlot
            {
                StartTime = parsedStart,
                EndTime = parsedEnd,
                Days = trimmedDays
            };

            _context.TimeSlots.Add(timeSlot);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Time slot created successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        [HttpPost]
        [Route("TimeSlots/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditTimeSlot(int timeSlotId, string startTime, string endTime, string days)
        {
            var timeSlot = await _context.TimeSlots.FindAsync(timeSlotId);
            if (timeSlot == null)
            {
                TempData["Error"] = "Time slot not found.";
                return RedirectToAction(nameof(CourseManagement));
            }

            if (!TimeOnly.TryParse(startTime, out var parsedStart) || !TimeOnly.TryParse(endTime, out var parsedEnd))
            {
                TempData["Error"] = "Invalid time format provided.";
                return RedirectToAction(nameof(CourseManagement));
            }

            string trimmedDays = string.IsNullOrWhiteSpace(days) ? "Monday - Friday" : days.Trim();

            bool exists = await _context.TimeSlots.AnyAsync(ts => ts.StartTime == parsedStart && ts.EndTime == parsedEnd && ts.Days.ToLower() == trimmedDays.ToLower() && ts.TimeSlotId != timeSlotId);
            if (exists)
            {
                TempData["Error"] = "Another time slot with this time range and days already exists.";
                return RedirectToAction(nameof(CourseManagement));
            }

            timeSlot.StartTime = parsedStart;
            timeSlot.EndTime = parsedEnd;
            timeSlot.Days = trimmedDays;

            await _context.SaveChangesAsync();

            TempData["Success"] = "Time slot updated successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        [HttpPost]
        [Route("TimeSlots/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTimeSlot(int id)
        {
            var timeSlot = await _context.TimeSlots.FindAsync(id);
            if (timeSlot == null)
            {
                TempData["Error"] = "Time slot not found.";
                return RedirectToAction(nameof(CourseManagement));
            }

            bool inUse = await _context.Batches.AnyAsync(b => b.TimeSlotId == id);
            if (inUse)
            {
                TempData["Error"] = "Cannot delete time slot because active batches are assigned to it.";
                return RedirectToAction(nameof(CourseManagement));
            }

            _context.TimeSlots.Remove(timeSlot);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Time slot deleted successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        // --- BATCHES ---

        [HttpPost]
        [Route("Batches/Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBatch(int classId, int subjectId, int courseTypeId, int timeSlotId, int capacity, bool isActive, string? batchName, int? teacherId)
        {
            var targetClass = await _context.Classes.FindAsync(classId);
            var targetSubject = await _context.Subjects.FindAsync(subjectId);
            if (targetClass == null || targetSubject == null)
            {
                TempData["Error"] = "Invalid Class or Subject selected.";
                return RedirectToAction(nameof(CourseManagement));
            }

            // Prevent duplicate batch (Same Class, Subject, CourseType, and TimeSlot)
            bool duplicateExists = await _context.Batches.AnyAsync(b =>
                b.ClassId == classId &&
                b.SubjectId == subjectId &&
                b.CourseTypeId == courseTypeId &&
                b.TimeSlotId == timeSlotId);

            if (duplicateExists)
            {
                TempData["Error"] = "A batch with the exact same Class, Subject, Course Type, and Time Slot already exists.";
                return RedirectToAction(nameof(CourseManagement));
            }

            int assignedTeacherId = teacherId ?? 0;

            // If teacher is selected, check double-booking
            if (assignedTeacherId > 0)
            {
                bool teacherCollision = await _context.Batches.AnyAsync(b =>
                    b.TeacherId == assignedTeacherId &&
                    b.TimeSlotId == timeSlotId &&
                    b.IsActive);

                if (teacherCollision)
                {
                    TempData["Error"] = "The selected teacher is already assigned to another active batch at this exact time slot.";
                    return RedirectToAction(nameof(CourseManagement));
                }
            }

            // Generate Batch Name if not provided
            int batchNum = await _context.Batches.CountAsync(b => b.SubjectId == subjectId) + 1;
            string generatedName = !string.IsNullOrWhiteSpace(batchName)
                ? batchName.Trim()
                : $"{targetClass.ClassName} - {targetSubject.SubjectName} (Batch {batchNum})";

            var batch = new Batch
            {
                BatchName = generatedName,
                ClassId = classId,
                SubjectId = subjectId,
                CourseTypeId = courseTypeId,
                TimeSlotId = timeSlotId,
                TeacherId = assignedTeacherId,
                Capacity = capacity > 0 ? capacity : 25,
                IsActive = isActive
            };

            _context.Batches.Add(batch);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Batch '{generatedName}' created successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        [HttpPost]
        [Route("Batches/Edit")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBatch(int batchId, int classId, int subjectId, int courseTypeId, int timeSlotId, int capacity, bool isActive, string batchName, int? teacherId)
        {
            var batch = await _context.Batches.FindAsync(batchId);
            if (batch == null)
            {
                TempData["Error"] = "Batch not found.";
                return RedirectToAction(nameof(CourseManagement));
            }

            // Prevent duplicate batch
            bool duplicateExists = await _context.Batches.AnyAsync(b =>
                b.BatchId != batchId &&
                b.ClassId == classId &&
                b.SubjectId == subjectId &&
                b.CourseTypeId == courseTypeId &&
                b.TimeSlotId == timeSlotId);

            if (duplicateExists)
            {
                TempData["Error"] = "Another batch with the exact same Class, Subject, Course Type, and Time Slot already exists.";
                return RedirectToAction(nameof(CourseManagement));
            }

            int assignedTeacherId = teacherId ?? 0;

            // Check teacher double booking
            if (assignedTeacherId > 0)
            {
                bool teacherCollision = await _context.Batches.AnyAsync(b =>
                    b.BatchId != batchId &&
                    b.TeacherId == assignedTeacherId &&
                    b.TimeSlotId == timeSlotId &&
                    b.IsActive);

                if (teacherCollision)
                {
                    TempData["Error"] = "The selected teacher is already assigned to another active batch at this time slot.";
                    return RedirectToAction(nameof(CourseManagement));
                }
            }

            batch.BatchName = string.IsNullOrWhiteSpace(batchName) ? batch.BatchName : batchName.Trim();
            batch.ClassId = classId;
            batch.SubjectId = subjectId;
            batch.CourseTypeId = courseTypeId;
            batch.TimeSlotId = timeSlotId;
            batch.TeacherId = assignedTeacherId;
            batch.Capacity = capacity > 0 ? capacity : 25;
            batch.IsActive = isActive;

            await _context.SaveChangesAsync();

            TempData["Success"] = $"Batch '{batch.BatchName}' updated successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        [HttpPost]
        [Route("Batches/Delete/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBatch(int id)
        {
            var batch = await _context.Batches.FindAsync(id);
            if (batch == null)
            {
                TempData["Error"] = "Batch not found.";
                return RedirectToAction(nameof(CourseManagement));
            }

            _context.Batches.Remove(batch);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Batch '{batch.BatchName}' deleted successfully.";
            return RedirectToAction(nameof(CourseManagement));
        }

        // --- TEACHER ASSIGNMENTS ---

        [HttpPost]
        [Route("TeacherAssignments/Assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AssignTeacher(int teacherId, int batchId)
        {
            var teacher = await _context.Users.FirstOrDefaultAsync(u => u.UserId == teacherId && u.Role == "Teacher");
            if (teacher == null)
            {
                TempData["Error"] = "Teacher not found or inactive.";
                return RedirectToAction(nameof(CourseManagement));
            }

            var batch = await _context.Batches.Include(b => b.TimeSlot).FirstOrDefaultAsync(b => b.BatchId == batchId);
            if (batch == null)
            {
                TempData["Error"] = "Batch not found.";
                return RedirectToAction(nameof(CourseManagement));
            }

            if (batch.TeacherId == teacherId)
            {
                TempData["Error"] = $"{teacher.FullName} is already assigned to this batch.";
                return RedirectToAction(nameof(CourseManagement));
            }

            // Prevent duplicate teacher assignment / schedule collision
            bool scheduleCollision = await _context.Batches.AnyAsync(b =>
                b.BatchId != batchId &&
                b.TeacherId == teacherId &&
                b.TimeSlotId == batch.TimeSlotId &&
                b.IsActive);

            if (scheduleCollision)
            {
                TempData["Error"] = $"Cannot assign {teacher.FullName}. Teacher already has an active class during this time slot.";
                return RedirectToAction(nameof(CourseManagement));
            }

            batch.TeacherId = teacherId;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"{teacher.FullName} successfully assigned to batch '{batch.BatchName}'.";
            return RedirectToAction(nameof(CourseManagement));
        }

        [HttpPost]
        [Route("TeacherAssignments/Unassign/{batchId:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnassignTeacher(int batchId)
        {
            var batch = await _context.Batches.Include(b => b.Teacher).FirstOrDefaultAsync(b => b.BatchId == batchId);
            if (batch == null || batch.TeacherId == 0)
            {
                TempData["Error"] = "No active teacher assignment found for this batch.";
                return RedirectToAction(nameof(CourseManagement));
            }

            string teacherName = batch.Teacher?.FullName ?? "Teacher";
            batch.TeacherId = 0;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Removed {teacherName} from batch '{batch.BatchName}'.";
            return RedirectToAction(nameof(CourseManagement));
        }

        // --- JSON APIs FOR AJAX CASCADING DROPDOWNS ---

        [HttpGet]
        [Route("GetSubjectsByClass/{classId:int}")]
        public async Task<IActionResult> GetSubjectsByClass(int classId)
        {
            var subjects = await _context.Subjects
                .Where(s => s.ClassId == classId && s.IsActive)
                .OrderBy(s => s.SubjectName)
                .Select(s => new
                {
                    subjectId = s.SubjectId,
                    subjectName = s.SubjectName
                })
                .ToListAsync();

            return Json(subjects);
        }

        [HttpGet]
        [Route("GetBatchesByClassAndSubject/{classId:int}/{subjectId:int}")]
        public async Task<IActionResult> GetBatchesByClassAndSubject(int classId, int subjectId)
        {
            var batches = await _context.Batches
                .Where(b => b.ClassId == classId && b.SubjectId == subjectId && b.IsActive)
                .OrderBy(b => b.BatchName)
                .Select(b => new
                {
                    batchId = b.BatchId,
                    batchName = b.BatchName,
                    teacherId = b.TeacherId,
                    teacherName = b.Teacher != null ? b.Teacher.FullName : "Unassigned"
                })
                .ToListAsync();

            return Json(batches);
        }
    }
}