using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers
{
    public class AccountController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
