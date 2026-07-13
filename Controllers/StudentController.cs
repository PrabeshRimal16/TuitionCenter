using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TuitionCenter.Models;

namespace TuitionCenter.Controllers
{
    public class StudentController : Controller
    {
        private readonly TuitionCenterDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public StudentController(TuitionCenterDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpGet]
        public IActionResult Dashboard()
        {
            return View();
        }
    }
}
