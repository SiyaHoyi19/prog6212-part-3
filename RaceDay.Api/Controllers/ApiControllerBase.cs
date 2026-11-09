using Microsoft.AspNetCore.Mvc;
using RaceDay.Api.Data;
namespace RaceDay.Api.Controllers;

[ApiController]
public abstract class ApiControllerBase(RaceDayDbContext db) : ControllerBase
{
    protected readonly RaceDayDbContext Db = db; protected int? CurrentUserId => HttpContext.Session.GetInt32("UserId"); protected string? CurrentRole => HttpContext.Session.GetString("Role"); protected IActionResult? RequireLogin() => CurrentUserId is null ? Unauthorized(new { message = "Login required." }) : null; protected IActionResult? RequireRole(string role)
    {
        if (CurrentUserId is null)
            return Unauthorized(new
            {
                message = "Login required."
            });
        return CurrentRole != role ? StatusCode(403, new
        {
            message = $"{role} role required."
        }) : null;
    }
}
