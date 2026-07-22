using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuitionCenter.Models;
using TuitionCenter.Models.ViewModels.Admin;

namespace TuitionCenter.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/Teachers")]
public class TeacherManagementController : Controller
{
    private readonly TuitionCenterDbContext _context;
    private readonly PasswordHasher<User> _passwordHasher;

    public TeacherManagementController(TuitionCenterDbContext context)
    {
        _context = context;
        _passwordHasher = new PasswordHasher<User>();
    }

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
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
            "~/Views/Admin/TeacherManagement/Index.cshtml",
            teachers
        );
    }

    [HttpGet]
    [Route("Create")]
    public IActionResult Create()
    {
        return View(
            "~/Views/Admin/TeacherManagement/Create.cshtml",
            new TeacherVM()
        );
    }

    [HttpPost]
    [Route("Create")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(TeacherVM model)
    {
        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Admin/TeacherManagement/Create.cshtml",
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
                "~/Views/Admin/TeacherManagement/Create.cshtml",
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

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
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
            "~/Views/Admin/TeacherManagement/Edit.cshtml",
            teacher
        );
    }

    [HttpPost]
    [Route("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
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
                "~/Views/Admin/TeacherManagement/Edit.cshtml",
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
                "~/Views/Admin/TeacherManagement/Edit.cshtml",
                model
            );
        }

        teacher.FullName = model.FullName;
        teacher.Email = model.Email;
        teacher.Phone = model.Phone;
        teacher.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Teacher details updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
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
            "~/Views/Admin/TeacherManagement/Details.cshtml",
            teacher
        );
    }

    [HttpPost]
    [Route("ToggleActive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
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

        return RedirectToAction(nameof(Index));
    }
}