using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

public sealed class AuthController(RaceDayApiClient api) : Controller
{
    [HttpGet] public IActionResult Login() => View(new LoginViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
            return View(model);
        using var response = await api.SendAsync(HttpMethod.Post, "/api/authentication/login",
            new LoginRequest(model.Email, model.Password), token);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ErrorMessageAsync(response, token));
            return View(model);
        }
        var user = await response.Content.ReadFromJsonAsync<AuthResponse>(cancellationToken: token);
        if (user is null)
        {
            ModelState.AddModelError("", "The API returned an incomplete sign-in response.");
            return View(model);
        }
        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("FullName", user.FullName);
        return user.Role.Equals("Organiser", StringComparison.OrdinalIgnoreCase)
            ? RedirectToAction("Index", "Organiser")
            : RedirectToAction("Index", "Participant");
    }

    [HttpGet] public IActionResult Register() => View(new RegisterViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
            return View(model);
        var request = new RegisterRequest(model.FullName, model.Email, model.Password, model.PhoneNumber,
            model.DateOfBirth, model.Role, model.Username);
        using var response = await api.SendAsync(HttpMethod.Post, "/api/authentication/register", request, token);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ErrorMessageAsync(response, token));
            return View(model);
        }
        TempData["Success"] = "Account created. Sign in to continue.";
        return RedirectToAction(nameof(Login));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout(CancellationToken token)
    {
        if (HttpContext.Session.GetInt32("UserId") is not null)
        {
            using var response = await api.SendAsync(HttpMethod.Post, "/api/authentication/logout", token: token);
        }
        HttpContext.Session.Clear();
        return RedirectToAction(nameof(Login));
    }

    public IActionResult Forbidden() => View();
}
