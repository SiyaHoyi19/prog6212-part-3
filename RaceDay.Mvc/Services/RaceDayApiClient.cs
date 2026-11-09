using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace RaceDay.Mvc.Services;

public class RaceDayApiClient(
    IHttpClientFactory factory,
    IHttpContextAccessor accessor)
{
    private const string ApiCookieKey = "RaceDay.ApiCookie";

    public async Task<HttpResponseMessage> SendAsync(
        HttpMethod method,
        string path,
        object? body = null,
        CancellationToken token = default)
    {
        var context = accessor.HttpContext
            ?? throw new InvalidOperationException("No active MVC request.");

        using var request = new HttpRequestMessage(method, path);

        var cookie = context.Session.GetString(ApiCookieKey)
            ?? context.Session.GetString("ApiSessionCookie");

        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        if (body is not null)
            request.Content = JsonContent.Create(body);

        var response = await factory
            .CreateClient("RaceDayApi")
            .SendAsync(request, token);

        if (response.Headers.TryGetValues("Set-Cookie", out var values))
        {
            var sessionCookie = values
                .Select(value => value.Split(';', 2)[0])
                .FirstOrDefault(value =>
                    value.StartsWith("RaceDay.Session=", StringComparison.OrdinalIgnoreCase)
                    || value.StartsWith(".AspNetCore.Session=", StringComparison.OrdinalIgnoreCase));

            if (sessionCookie is not null)
                context.Session.SetString(ApiCookieKey, sessionCookie);
        }

        if (path.TrimEnd('/').EndsWith("/logout", StringComparison.OrdinalIgnoreCase)
            && response.IsSuccessStatusCode)
        {
            context.Session.Remove(ApiCookieKey);
        }

        return response;
    }

    public async Task<HttpResponseMessage> SendFileAsync(
        HttpMethod method,
        string path,
        IFormFile file,
        CancellationToken token = default)
    {
        var context = accessor.HttpContext
            ?? throw new InvalidOperationException("No active MVC request.");

        using var request = new HttpRequestMessage(method, path);
        using var form = new MultipartFormDataContent();
        await using var stream = file.OpenReadStream();
        using var fileContent = new StreamContent(stream);

        if (MediaTypeHeaderValue.TryParse(file.ContentType, out var contentType))
            fileContent.Headers.ContentType = contentType;

        form.Add(fileContent, "file", Path.GetFileName(file.FileName));
        request.Content = form;

        var cookie = context.Session.GetString(ApiCookieKey)
            ?? context.Session.GetString("ApiSessionCookie");

        if (!string.IsNullOrWhiteSpace(cookie))
            request.Headers.TryAddWithoutValidation("Cookie", cookie);

        return await factory
            .CreateClient("RaceDayApi")
            .SendAsync(request, token);
    }

    public static async Task<string> ErrorMessageAsync(
        HttpResponseMessage response,
        CancellationToken token = default)
    {
        try
        {
            using var document = await JsonDocument.ParseAsync(
                await response.Content.ReadAsStreamAsync(token),
                cancellationToken: token);

            if (document.RootElement.TryGetProperty("message", out var message))
                return message.GetString() ?? "The API rejected the request.";

            if (document.RootElement.TryGetProperty("title", out var title))
                return title.GetString() ?? "The API rejected the request.";
        }
        catch (JsonException)
        {
            // The API response was not JSON.
        }

        return $"The API returned HTTP {(int)response.StatusCode}.";
    }
}
