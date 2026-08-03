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
        // Subject Course management
        // =====================================================
        
    }
}