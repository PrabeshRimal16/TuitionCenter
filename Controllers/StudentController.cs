using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TuitionCenter.Models;

namespace TuitionCenter.Controllers
{
    public class StudentController : Controller
    {
        private readonly TuitionCenterDbContext _context;

        public StudentController(TuitionCenterDbContext context)
        {
            _context = context;
        }

        public IActionResult Dashboard()
        {
            return View();
        }

        public async Task<IActionResult> Class()
        {
            return View(await _context.Classes.ToListAsync());
        }

        public async Task<IActionResult> Subject()
        {
            return View(await _context.Subjects
                .Include(x => x.Class)
                .ToListAsync());
        }

        public async Task<IActionResult> Intake()
        {
            return View(await _context.Batches
                .Include(x => x.Class)
                .Include(x => x.Subject)
                .ToListAsync());
        }

        public async Task<IActionResult> Pricing()
        {
            return View(await _context.CourseFees
                .Include(x => x.Class)
                .Include(x => x.Subject)
                .Include(x => x.CourseType)
                .Where(x => x.IsActive)
                .ToListAsync());
        }

        public async Task<IActionResult> Timing()
        {
            return View(await _context.Batches
                .Include(x => x.Class)
                .Include(x => x.Subject)
                .ToListAsync());
        }

        public async Task<IActionResult> Payments()
        {
            return View();
        }
    }
}