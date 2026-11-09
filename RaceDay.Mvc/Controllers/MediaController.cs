using Microsoft.AspNetCore.Mvc;
using RaceDay.Mvc.Services;

namespace RaceDay.Mvc.Controllers;

[Route("media")]
public sealed class MediaController(RaceDayApiClient api) : Controller
{
    [HttpGet("events/{id:int}/banner")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> EventBanner(int id, CancellationToken token) =>
        ProxyImageAsync($"/api/events/{id}/banner/image", token);

    [HttpGet("profile")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public Task<IActionResult> ProfileImage(CancellationToken token) =>
        ProxyImageAsync("/api/profile/me/image", token);

    private async Task<IActionResult> ProxyImageAsync(string path, CancellationToken token)
    {
        if (HttpContext.Session.GetInt32("UserId") is null)
            return Unauthorized();

        try
        {
            using var response = await api.SendAsync(HttpMethod.Get, path, token: token);
            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode);

            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var bytes = await response.Content.ReadAsByteArrayAsync(token);
            return File(bytes, contentType);
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status503ServiceUnavailable);
        }
    }
}
