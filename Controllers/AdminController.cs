using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using TuitionCenter.Models;

namespace TuitionCenter.Controllers
{
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
        public IActionResult Dashboard()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Student()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Teacher()
        {
            return View();
        }


    }
}
