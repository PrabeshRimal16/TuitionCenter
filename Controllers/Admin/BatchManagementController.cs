using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin
{
    public class BatchManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
