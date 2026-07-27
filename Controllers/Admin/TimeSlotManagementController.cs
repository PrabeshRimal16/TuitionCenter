using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin
{
    public class TimeSlotManagementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
