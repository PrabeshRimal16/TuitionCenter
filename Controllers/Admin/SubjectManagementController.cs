using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin
{
    public class SubjectManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
