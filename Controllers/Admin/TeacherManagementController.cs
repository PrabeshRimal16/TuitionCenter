using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace TuitionCenter.Controllers.Admin;

[Authorize(Roles = "Admin")]
public class TeacherManagementController : Controller
{
    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Create()
    {
        return View();
    }

    public IActionResult Edit(int id)
    {
        return View();
    }

    public IActionResult Details(int id)
    {
        return View();
    }
}