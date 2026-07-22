using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin
{
    public class AnnouncementController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
