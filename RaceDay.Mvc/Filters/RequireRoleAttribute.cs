using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace RaceDay.Mvc.Filters;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireRoleAttribute(string role) : Attribute, IAsyncActionFilter
{
    public async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var session = context.HttpContext.Session;
        if (session.GetInt32("UserId") is null)
        {
            context.Result = new RedirectToActionResult("Login", "Auth", null);
            return;
        }
        if (!string.Equals(session.GetString("Role"), role, StringComparison.OrdinalIgnoreCase))
        {
            context.Result = new RedirectToActionResult("Forbidden", "Auth", null);
            return;
        }
        await next();
    }
}
