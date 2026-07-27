using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin
{
    public class ClassManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
