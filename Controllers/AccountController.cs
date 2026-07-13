using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TuitionCenter.Models;

namespace TuitionCenter.Controllers
{
    public class AccountController : Controller
    {
        // TODO: rename to match your actual DbContext class/namespace if different
        private readonly TuitionCenterDbContext _context;
        private readonly PasswordHasher<User> _passwordHasher;

        public AccountController(TuitionCenterDbContext context)
        {
            _context = context;
            _passwordHasher = new PasswordHasher<User>();
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public IActionResult Register(UserListEdit u)
        {
            if (!ModelState.IsValid)
            {
                return View(u);
            }

            try
            {
                // Check if email already exists
                var existingUser = _context.Users
                    .FirstOrDefault(x => x.Email.ToUpper() == u.Email.ToUpper());

                if (existingUser != null)
                {
                    ModelState.AddModelError("", "User already exists with this email.");
                    return View(u);
                }

                // Create User entity
                User userEntity = new User()
                {
                    FullName = u.FullName,
                    Email = u.Email,
                    Phone = u.Phone,
                    Role = u.Role,
                    IsActive = true,
                    CreatedDate = DateTime.Now,
                };

                // Hash the password (one-way, salted) instead of reversible encryption
                userEntity.PasswordHash = _passwordHasher.HashPassword(userEntity, u.PasswordHash);

                _context.Users.Add(userEntity);
                _context.SaveChanges();

                return RedirectToAction("Login");
            }
            catch
            {
                ModelState.AddModelError("", "Registration failed. Please try again.");
                return View(u);
            }
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }

        [AllowAnonymous]
        [HttpPost]
        public async Task<IActionResult> Login(UserListEdit u)
        {
/*            if (!ModelState.IsValid)
            {
                return View(u);
            }
*/
            try
            {
                var user = _context.Users
                    .FirstOrDefault(x => x.Email.ToUpper() == u.Email.ToUpper());

                if (user == null)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(u);
                }

                var verifyResult = _passwordHasher.VerifyHashedPassword(
                    user, user.PasswordHash, u.PasswordHash);

                if (verifyResult == PasswordVerificationResult.Failed)
                {
                    ModelState.AddModelError("", "Invalid email or password.");
                    return View(u);
                }

                if (user.IsActive == false)
                {
                    ModelState.AddModelError("", "This account has been deactivated.");
                    return View(u);
                }

                // Optional: transparently upgrade the hash if the algorithm was rehashed
                if (verifyResult == PasswordVerificationResult.SuccessRehashNeeded)
                {
                    user.PasswordHash = _passwordHasher.HashPassword(user, u.PasswordHash);
                    _context.SaveChanges();
                }

                List<Claim> claims = new()
                {
                    /*new Claim(ClaimTypes.Name, user.FullName),*/
                    new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                    new Claim(ClaimTypes.Role, user.Role),
                    new Claim("Email", user.Email),
                    new Claim("Phone", user.Phone ?? "")
                };

                ClaimsIdentity identity = new ClaimsIdentity(
                    claims,
                    CookieAuthenticationDefaults.AuthenticationScheme);

                ClaimsPrincipal principal = new ClaimsPrincipal(identity);

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    principal);

                return user.Role switch
                {
                    "Admin" => RedirectToAction("Dashboard", "Admin"),
                    "Teacher" => RedirectToAction("Dashboard", "Teacher"),
                    "Student" => RedirectToAction("Dashboard", "Student"),
                    _ => RedirectToAction("Index", "Home")
                };
            }
            catch
            {
                ModelState.AddModelError("", "Login failed. Please try again.");
                return View(u);
            }
        }

        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(
                CookieAuthenticationDefaults.AuthenticationScheme);

            return RedirectToAction("Login", "Account");
        }
    }
}