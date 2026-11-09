using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using RaceDay.Contracts;
using RaceDay.Mvc.Filters;
using RaceDay.Mvc.Models;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[RequireRole("Organiser")]
public sealed class OrganiserController(RaceDayApiClient api) : Controller
{
    public async Task<IActionResult> Index(CancellationToken token)
    {
        var events = await ReadAsync<List<EventDto>>("/api/events/mine", token) ?? [];
        var model = new OrganiserDashboardViewModel();
        foreach (var item in events)
        {
            var enrolments = await ReadAsync<List<EnrolmentDto>>($"/api/events/{item.EventId}/enrolments", token) ?? [];
            model.Events.Add(new OrganiserEventSummaryViewModel { Event = item, EnrolmentCount = enrolments.Count });
        }
        return View(model);
    }

    public async Task<IActionResult> Events(CancellationToken token)
    {
        ViewBag.Error = TempData["Error"];
        ViewBag.Success = TempData["Success"];
        return View(await ReadAsync<List<EventDto>>("/api/events/mine", token) ?? []);
    }

    [HttpGet]
    public IActionResult CreateEvent() => View("EventForm", new EventEditViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateEvent(EventEditViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
            return View("EventForm", model);
        var request = new CreateEventRequest(model.Name, model.Description, model.Date, model.Location, model.Distance, model.Type);
        using var response = await api.SendAsync(HttpMethod.Post, "/api/events", request, token);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ErrorMessageAsync(response, token));
            return View("EventForm", model);
        }
        var created = await response.Content.ReadFromJsonAsync<EventDto>(cancellationToken: token);
        if (model.Banner is not null && created is not null)
        {
            using var upload = await api.SendFileAsync(HttpMethod.Post,
                $"/api/events/{created.EventId}/banner", model.Banner, token);
            if (!upload.IsSuccessStatusCode)
            {
                TempData["Error"] = "The event was created, but its banner was not uploaded: "
                    + await RaceDayApiClient.ErrorMessageAsync(upload, token);
                return RedirectToAction(nameof(Events));
            }
        }
        TempData["Success"] = "Event created.";
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> EditEvent(int id, CancellationToken token)
    {
        var item = await ReadAsync<EventDto>($"/api/events/{id}", token);
        if (item is null)
            return NotFound();
        return View("EventForm", new EventEditViewModel
        {
            EventId = item.EventId,
            Name = item.Name,
            Description = item.Description,
            Date = item.Date,
            Location = item.Location,
            Distance = item.Distance,
            Type = item.Type,
            BannerImageUrl = item.BannerImageUrl
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> EditEvent(int id, EventEditViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
            return View("EventForm", model);
        var request = new UpdateEventRequest(model.Name, model.Description, model.Date, model.Location, model.Distance, model.Type);
        using var response = await api.SendAsync(HttpMethod.Put, $"/api/events/{id}", request, token);
        if (!response.IsSuccessStatusCode)
        {
            ModelState.AddModelError("", await RaceDayApiClient.ErrorMessageAsync(response, token));
            return View("EventForm", model);
        }
        if (model.Banner is not null)
        {
            using var upload = await api.SendFileAsync(HttpMethod.Post,
                $"/api/events/{id}/banner", model.Banner, token);
            if (!upload.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "The event was updated, but its banner was not uploaded: "
                    + await RaceDayApiClient.ErrorMessageAsync(upload, token));
                return View("EventForm", model);
            }
        }
        TempData["Success"] = "Event updated.";
        return RedirectToAction(nameof(Events));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteEvent(int id, CancellationToken token)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, $"/api/events/{id}", token: token);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
            response.IsSuccessStatusCode ? "Event deleted." : await RaceDayApiClient.ErrorMessageAsync(response, token);
        return RedirectToAction(nameof(Events));
    }

    [HttpGet]
    public async Task<IActionResult> Categories(int eventId, CancellationToken token)
    {
        var item = await ReadAsync<EventDto>($"/api/events/{eventId}", token);
        if (item is null)
            return NotFound();
        ViewBag.Event = item;
        ViewBag.Error = TempData["Error"];
        ViewBag.Success = TempData["Success"];
        return View(await ReadAsync<List<CategoryDto>>($"/api/events/{eventId}/categories", token) ?? []);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveCategory(CategoryEditViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Enter a category name, distance, and valid minimum age.";
            return RedirectToAction(nameof(Categories), new
            {
                eventId = model.EventId
            });
        }
        var request = new CreateCategoryRequest(model.Name, model.Distance, model.MinAge);
        var method = model.CategoryId.HasValue ? HttpMethod.Put : HttpMethod.Post;
        var path = model.CategoryId.HasValue
            ? $"/api/categories/{model.CategoryId.Value}"
            : $"/api/events/{model.EventId}/categories";
        using var response = await api.SendAsync(method, path, request, token);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
            response.IsSuccessStatusCode ? "Category saved." : await RaceDayApiClient.ErrorMessageAsync(response, token);
        return RedirectToAction(nameof(Categories), new
        {
            eventId = model.EventId
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteCategory(int eventId, int id, CancellationToken token)
    {
        using var response = await api.SendAsync(HttpMethod.Delete, $"/api/categories/{id}", token: token);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
            response.IsSuccessStatusCode ? "Category deleted." : await RaceDayApiClient.ErrorMessageAsync(response, token);
        return RedirectToAction(nameof(Categories), new
        {
            eventId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Enrolments(int eventId, CancellationToken token)
    {
        var item = await ReadAsync<EventDto>($"/api/events/{eventId}", token);
        if (item is null)
            return NotFound();
        var rows = await BuildRowsAsync(eventId, token);
        return View(new OrganiserEnrolmentsViewModel { Event = item, Enrolments = rows });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateEnrolmentStatus(int eventId, int id, string status, CancellationToken token)
    {
        using var response = await api.SendAsync(HttpMethod.Put, $"/api/enrolments/{id}/status",
            new UpdateEnrolmentStatusRequest(status), token);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
            response.IsSuccessStatusCode ? "Enrolment status updated." : await RaceDayApiClient.ErrorMessageAsync(response, token);
        return RedirectToAction(nameof(Enrolments), new
        {
            eventId
        });
    }

    [HttpGet]
    public async Task<IActionResult> Results(int eventId, CancellationToken token)
    {
        var item = await ReadAsync<EventDto>($"/api/events/{eventId}", token);
        if (item is null)
            return NotFound();
        return View(new OrganiserResultsViewModel
        {
            Event = item,
            Enrolments = await BuildRowsAsync(eventId, token)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SaveResult(ResultEntryViewModel model, CancellationToken token)
    {
        if (!ModelState.IsValid)
        {
            TempData["Error"] = "Check the finish time, position, and result status.";
            return RedirectToAction(nameof(Results), new
            {
                eventId = model.EventId
            });
        }
        var request = new CreateResultRequest(model.EnrolmentId, model.FinishTime, model.Position, model.ResultStatus);
        using var response = await api.SendAsync(HttpMethod.Post,
            $"/api/enrolments/{model.EnrolmentId}/results", request, token);
        TempData[response.IsSuccessStatusCode ? "Success" : "Error"] =
            response.IsSuccessStatusCode ? "Result recorded." : await RaceDayApiClient.ErrorMessageAsync(response, token);
        return RedirectToAction(nameof(Results), new
        {
            eventId = model.EventId
        });
    }

    private async Task<List<OrganiserEnrolmentRowViewModel>> BuildRowsAsync(int eventId, CancellationToken token)
    {
        var enrolments = await ReadAsync<List<OrganiserEnrolmentDto>>($"/api/events/{eventId}/enrolments", token) ?? [];
        var results = await ReadAsync<List<ResultDto>>($"/api/events/{eventId}/results", token) ?? [];
        return enrolments.Select(enrolment => new OrganiserEnrolmentRowViewModel
        {
            Enrolment = enrolment,
            CategoryName = enrolment.CategoryName,
            Result = results.FirstOrDefault(result => result.EnrolmentId == enrolment.EnrolmentId)
        }).ToList();
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

