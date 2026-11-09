using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RequireRole("Participant")]
public sealed class ParticipantController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(string? type, string? location, string? search, CancellationToken token)
    {
        var events = await ReadAsync<List<EventDto>>("/api/events", token) ?? [];
        var today = DateOnly.FromDateTime(DateTime.Today);
        events = events.Where(item => item.Date >= today).ToList();
        if (!string.IsNullOrWhiteSpace(type))
            events = events.Where(item => item.Type.Equals(type, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(location))
            events = events.Where(item => item.Location.Contains(location, StringComparison.OrdinalIgnoreCase)).ToList();
        if (!string.IsNullOrWhiteSpace(search))
            events = events.Where(item => item.Name.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (item.Description?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || item.Location.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        return View(new ParticipantEventsViewModel
        {
            Events = events.OrderBy(item => item.Date).ToList(),
            Type = type,
            Location = location,
            Search = search
        });
    }

    [HttpGet]
    public async Task<IActionResult> Details(int id, CancellationToken token)
    {
        var item = await ReadAsync<EventDto>($"/api/events/{id}", token);
        if (item is null)
            return NotFound();
        var categories = await ReadAsync<List<CategoryDto>>($"/api/events/{id}/categories", token) ?? [];
        ViewBag.Error = TempData["Error"];
        ViewBag.Success = TempData["Success"];
        return View(new ParticipantEventDetailsViewModel { Event = item, Categories = categories });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EnterEvent(int eventId, int categoryId, CancellationToken token)
    {
        var request = new CreateEnrolmentRequest(eventId, categoryId);
        using var response = await api.SendAsync(HttpMethod.Post,
            $"/api/events/{eventId}/enrolments", request, token);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "You have entered this event. Your enrolment is pending organiser confirmation."
            : await RaceDayApiClient.ErrorMessageAsync(response, token);
        return RedirectToAction(nameof(Details), new
        {
            id = eventId
        });
    }

    [HttpGet]
    public async Task<IActionResult> MyEvents(CancellationToken token)
    {
        var enrolments = await ReadAsync<List<EnrolmentDto>>("/api/users/enrolments", token) ?? [];
        var events = await ReadAsync<List<EventDto>>("/api/events", token) ?? [];
        var rows = new List<ParticipantEnrolmentRowViewModel>();
        foreach (var enrolment in enrolments)
        {
            var eventItem = events.FirstOrDefault(item => item.EventId == enrolment.EventId);
            var categories = await ReadAsync<List<CategoryDto>>($"/api/events/{enrolment.EventId}/categories", token) ?? [];
            rows.Add(new ParticipantEnrolmentRowViewModel
            {
                Enrolment = enrolment,
                Event = eventItem,
                CategoryName = categories.FirstOrDefault(item => item.CategoryId == enrolment.CategoryId)?.Name ?? "Category"
            });
        }
        ViewBag.Error = TempData["Error"];
        return View(rows);
    }

    [HttpGet]
    public async Task<IActionResult> MyResults(CancellationToken token)
    {
        var results = await ReadAsync<List<ResultDto>>("/api/user/results", token) ?? [];
        var enrolments = await ReadAsync<List<EnrolmentDto>>("/api/users/enrolments", token) ?? [];
        var events = await ReadAsync<List<EventDto>>("/api/events", token) ?? [];
        var rows = new List<ParticipantResultRowViewModel>();
        foreach (var result in results)
        {
            var enrolment = enrolments.FirstOrDefault(item => item.EnrolmentId == result.EnrolmentId);
            if (enrolment is null)
                continue;
            var eventItem = events.FirstOrDefault(item => item.EventId == enrolment.EventId);
            var categories = await ReadAsync<List<CategoryDto>>($"/api/events/{enrolment.EventId}/categories", token) ?? [];
            rows.Add(new ParticipantResultRowViewModel
            {
                Result = result,
                Event = eventItem,
                CategoryName = categories.FirstOrDefault(item => item.CategoryId == enrolment.CategoryId)?.Name ?? "Category"
            });
        }
        ViewBag.Error = TempData["Error"];
        return View(rows.OrderByDescending(row => row.Event?.Date).ToList());
    }

    [HttpGet]
    public async Task<IActionResult> Profile(CancellationToken token)
    {
        var profile = await ReadAsync<ParticipantProfileViewModel>("/api/profile/me", token);
        if (profile is null)
            return View(new ParticipantProfileViewModel());
        return View(profile);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Profile(ParticipantProfileViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
            return View(model);
        var request = new UpdateProfileRequest(model.FullName, model.Email, model.PhoneNumber, model.DateOfBirth);
        using var response = await api.SendAsync(HttpMethod.Put, "/api/profile/me", request, token);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ErrorMessageAsync(response, token));
            return View(model);
        }
        HttpContext.Session.SetString("FullName", model.FullName);
        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Profile));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UploadProfileImage(IFormFile? file, CancellationToken token)
    {
        if (file is null)
        {
            TempData["Error"] = "Choose an image to upload.";
            return RedirectToAction(nameof(Profile));
        }

        using var response = await api.SendFileAsync(HttpMethod.Post, "/api/profile/me/image", file, token);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] = response.IsSuccessStatusCode
            ? "Profile picture uploaded."
            : await RaceDayApiClient.ErrorMessageAsync(response, token);
        return RedirectToAction(nameof(Profile));
    }

    private async Task<T?> ReadAsync<T>(string path, CancellationToken token)
    {
        try
        {
            using var response = await api.SendAsync(HttpMethod.Get, path, token: token);
            if (!response.IsSuccessStatusCode)
            {
                TempData["Error"] = await RaceDayApiClient.ErrorMessageAsync(response, token);
                return default;
            }
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: token);
        }
        catch (HttpRequestException)
        {
            TempData["Error"] = "Could not reach the RaceDay API. Check that it is running.";
            return default;
        }
    }
}
