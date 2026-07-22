using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin
{
    public class TeacherManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
