using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Student
{
    public class Student : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
