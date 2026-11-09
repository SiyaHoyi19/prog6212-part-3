using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Models;

namespace RaceDay.Mvc.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        if (HttpContext.Session.GetInt32("UserId") is null) return RedirectToAction("Login", "Auth");
        return string.Equals(HttpContext.Session.GetString("Role"), "Organiser", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Participant");
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() => View(new ErrorViewModel { RequestId = HttpContext.TraceIdentifier });
}
