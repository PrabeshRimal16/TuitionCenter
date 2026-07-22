using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin
{
    public class StudentManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
