using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Api.Services;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[Route("api/events")]
public class EventsController(RaceDayDbContext db) : ApiControllerBase(db)
{
    private static EventDto D(Event x) =>
        new(x.EventId, x.OrganiserId, x.Name, x.Description, x.Date,
            x.Location, x.Distance, x.Type, x.BannerImageUrl);

    [HttpGet]
    [SwaggerOperation(Summary = "List events", Description = "Available to signed-in Organisers and Participants. Returns event details and any saved banner URL.")]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAll()
    {
        if (RequireLogin() is IActionResult error)
            return error;

        var events = await Db.Events
            .Select(x => new EventDto(
                x.EventId, x.OrganiserId, x.Name, x.Description, x.Date,
                x.Location, x.Distance, x.Type, x.BannerImageUrl))
            .ToListAsync();

        return Ok(events);
    }

    [HttpGet("{id:int}")]
    [SwaggerOperation(Summary = "Get an event", Description = "Returns one event by its event ID for a signed-in user.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get(int id)
    {
        if (RequireLogin() is IActionResult error)
            return error;

        var item = await Db.Events.FindAsync(id);
        return item is null ? NotFound() : Ok(D(item));
    }

    [HttpGet("mine")]
    [SwaggerOperation(Summary = "List your events", Description = "Organiser only. Returns events created by the signed-in Organiser.")]
    [ProducesResponseType(typeof(List<EventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        if (RequireRole("Organiser") is IActionResult error)
            return error;

        var events = await Db.Events
            .Where(x => x.OrganiserId == CurrentUserId)
            .Select(x => new EventDto(
                x.EventId, x.OrganiserId, x.Name, x.Description, x.Date,
                x.Location, x.Distance, x.Type, x.BannerImageUrl))
            .ToListAsync();

        return Ok(events);
    }

    [HttpPost]
    [SwaggerOperation(Summary = "Create an event", Description = "Organiser only. Supply a name, date, location, positive distance, and event type Run, Walk, or Cycle. Description is optional.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create(CreateEventRequest request)
    {
        if (RequireRole("Organiser") is IActionResult error)
            return error;

        if (!ModelState.IsValid || request.Distance <= 0 || !ValidType(request.Type))
        {
            return BadRequest(new
            {
                message = "Valid name, date, location, positive distance and Run/Walk/Cycle type required."
            });
        }

        var item = new Event
        {
            OrganiserId = CurrentUserId!.Value,
            Name = request.Name.Trim(),
            Description = request.Description,
            Date = request.Date,
            Location = request.Location.Trim(),
            Distance = request.Distance,
            Type = Normalize(request.Type)
        };

        Db.Events.Add(item);
        await Db.SaveChangesAsync();

        return CreatedAtAction(nameof(Get), new
        {
            id = item.EventId
        }, D(item));
    }

    [HttpPut("{id:int}")]
    [SwaggerOperation(Summary = "Update an owned event", Description = "Organiser only. Replaces the event fields; the event must belong to the signed-in Organiser. Event type must be Run, Walk, or Cycle and distance must be positive.")]
    [ProducesResponseType(typeof(EventDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, UpdateEventRequest request)
    {
        if (RequireRole("Organiser") is IActionResult error)
            return error;

        if (!ModelState.IsValid || request.Distance <= 0 || !ValidType(request.Type))
            return BadRequest();

        var item = await Db.Events.FindAsync(id);
        if (item is null)
            return NotFound();

        if (item.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden);

        item.Name = request.Name.Trim();
        item.Description = request.Description;
        item.Date = request.Date;
        item.Location = request.Location.Trim();
        item.Distance = request.Distance;
        item.Type = Normalize(request.Type);

        await Db.SaveChangesAsync();
        return Ok(D(item));
    }

    [HttpDelete("{id:int}")]
    [SwaggerOperation(Summary = "Delete an owned event", Description = "Organiser only. Returns 409 Conflict while the event has enrolments; delete is limited to the event owner.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        if (RequireRole("Organiser") is IActionResult error)
            return error;

        var item = await Db.Events.FindAsync(id);
        if (item is null)
            return NotFound();

        if (item.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden);

        if (await Db.Enrolments.AnyAsync(enrolment => enrolment.EventId == id))
        {
            return Conflict(new
            {
                message = "This event has enrolments and cannot be deleted."
            });
        }

        Db.Events.Remove(item);
        await Db.SaveChangesAsync();

        return Ok(new
        {
            message = "Event deleted."
        });
    }

    [HttpPost("{id:int}/banner")]
    [Consumes("multipart/form-data")]
    [RequestSizeLimit(5 * 1024 * 1024)]
    [SwaggerOperation(Summary = "Upload an event banner", Description = "Organiser owner only. Send one JPG, PNG, or WebP image, up to 5 MB, as multipart form field 'file'. The API stores it in Azure Blob Storage and saves its URL on the event.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UploadBanner(
        int id,
        IFormFile? file,
        [FromServices] BlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        if (RequireRole("Organiser") is IActionResult error)
            return error;

        if (!BlobStorageService.IsValidImage(file))
        {
            return BadRequest(new
            {
                message = "Choose a JPG, PNG, or WebP image smaller than 5 MB."
            });
        }

        var item = await Db.Events.FindAsync(new object[] { id }, cancellationToken);
        if (item is null)
            return NotFound();

        if (item.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden);

        item.BannerImageUrl = await blobStorage.UploadAsync(
            file!,
            "event-banners",
            cancellationToken);

        await Db.SaveChangesAsync(cancellationToken);

        return Ok(new
        {
            imageUrl = item.BannerImageUrl
        });
    }

    [HttpGet("{id:int}/banner/image")]
    [SwaggerOperation(Summary = "Get an event banner", Description = "Returns the stored banner image for an event. Requires a signed-in user.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> BannerImage(
        int id,
        [FromServices] BlobStorageService blobStorage,
        CancellationToken cancellationToken)
    {
        if (RequireLogin() is IActionResult error)
            return error;

        var item = await Db.Events.FindAsync(new object[] { id }, cancellationToken);
        if (item?.BannerImageUrl is null)
            return NotFound();

        var image = await blobStorage.DownloadAsync(
            item.BannerImageUrl,
            cancellationToken);

        return image is null
            ? NotFound()
            : File(image.Content, image.ContentType);
    }

    private static bool ValidType(string type) =>
        new[] { "run", "walk", "cycle" }
            .Contains(type, StringComparer.OrdinalIgnoreCase);

    private static string Normalize(string type) =>
        char.ToUpperInvariant(type[0]) + type[1..].ToLowerInvariant();
}
