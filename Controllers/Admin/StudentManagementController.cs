using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuitionCenter.Models;
using TuitionCenter.Models.ViewModels.Admin;

namespace TuitionCenter.Controllers.Admin;

[Authorize(Roles = "Admin")]
[Route("Admin/Students")]
public class StudentManagementController : Controller
{
    private readonly TuitionCenterDbContext _context;

    public StudentManagementController(
        TuitionCenterDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    [Route("")]
    [Route("Index")]
    public async Task<IActionResult> Index()
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
            "~/Views/Admin/StudentManagement/Index.cshtml",
            students
        );
    }

    [HttpGet]
    [Route("Edit/{id:int}")]
    public async Task<IActionResult> Edit(int id)
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
            "~/Views/Admin/StudentManagement/Edit.cshtml",
            student
        );
    }

    [HttpPost]
    [Route("Edit/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        int id,
        StudentVM model)
    {
        if (id != model.UserId)
            return BadRequest();

        if (!ModelState.IsValid)
        {
            return View(
                "~/Views/Admin/StudentManagement/Edit.cshtml",
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
                "~/Views/Admin/StudentManagement/Edit.cshtml",
                model
            );
        }

        student.FullName = model.FullName;
        student.Email = model.Email;
        student.Phone = model.Phone;
        student.IsActive = model.IsActive;

        await _context.SaveChangesAsync();

        TempData["Success"] =
            "Student details updated successfully.";

        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    [Route("Details/{id:int}")]
    public async Task<IActionResult> Details(int id)
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

        student.Enrollments =
            await _context.Enrollments
            .Where(e => e.StudentId == id)
            .Select(e => new StudentEnrollmentVM
            {
                EnrollmentId = e.EnrollmentId,
                //EnrollmentDate = e.EnrollmentDate,
                EnrollmentStatus = e.Status,
               // CourseType = e.CourseType
            })
            .ToListAsync();

        return View(
            "~/Views/Admin/StudentManagement/Details.cshtml",
            student
        );
    }

    [HttpPost]
    [Route("ToggleActive/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(int id)
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

        return RedirectToAction(nameof(Index));
    }
}