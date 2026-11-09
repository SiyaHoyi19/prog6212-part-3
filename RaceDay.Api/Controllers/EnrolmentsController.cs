using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
namespace RaceDay.Api.Controllers;

[Route("api")]
public class EnrolmentsController(RaceDayDbContext db) : ApiControllerBase(db)
{
    static EnrolmentDto D(Enrolment x) => new(x.EnrolmentId, x.UserId, x.EventId, x.CategoryId, x.Date, x.Status);
    [HttpPost("events/{eventId:int}/enrolments")]
    [SwaggerOperation(Summary = "Enrol in an event", Description = "Participant only. The body must contain the same eventId as the route and a categoryId belonging to that event. A Participant may enrol once per event; new enrolments start as Pending.")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int eventId, CreateEnrolmentRequest r)
    {
        if (RequireRole("Participant") is IActionResult e)
            return e;
        if (eventId != r.EventId)
            return BadRequest(new
            {
                message = "Enrol for an event and choose a category"
            });
        var ev = await Db.Events.FindAsync(eventId);
        var cat = await Db.Categories.FindAsync(r.CategoryId);
        if (ev is null || cat is null || cat.EventId != eventId)
            return NotFound(new
            {
                message = "Event or category not found for this event."
            });
        if (await Db.Enrolments.AnyAsync(x => x.UserId == CurrentUserId && x.EventId == eventId))
            return Conflict(new
            {
                message = "Already enrolled in this event."
            });
        var x = new Enrolment { UserId = CurrentUserId!.Value, EventId = eventId, CategoryId = r.CategoryId, Date = DateOnly.FromDateTime(DateTime.UtcNow), Status = "Pending" };
        Db.Enrolments.Add(x);
        await Db.SaveChangesAsync();
        return Created("/api/users/enrolments", D(x));
    }
    [HttpGet("users/enrolments")]
    [SwaggerOperation(Summary = "List your enrolments", Description = "Participant only. Returns enrolments for the signed-in Participant, including category and current status.")]
    [ProducesResponseType(typeof(List<EnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        if (RequireRole("Participant") is IActionResult e)
            return e;
        return Ok(await Db.Enrolments.Where(x => x.UserId == CurrentUserId).Select(x => new EnrolmentDto(x.EnrolmentId, x.UserId, x.EventId, x.CategoryId, x.Date, x.Status)).ToListAsync());
    }
    [HttpGet("events/{eventId:int}/enrolments")]
    [SwaggerOperation(Summary = "List enrolments for an owned event", Description = "Organiser only. Returns participant and category details for an event owned by the signed-in Organiser.")]
    [ProducesResponseType(typeof(List<OrganiserEnrolmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        var ev = await Db.Events.FindAsync(eventId);
        if (ev is null)
            return NotFound();
        if (ev.OrganiserId != CurrentUserId)
            return StatusCode(StatusCodes.Status403Forbidden);
        return Ok(await Db.Enrolments.Where(x => x.EventId == eventId).Select(x => new OrganiserEnrolmentDto(x.EnrolmentId, x.UserId, x.User!.FullName, x.EventId, x.CategoryId, x.Category!.Name, x.Date, x.Status)).ToListAsync());
    }
    [HttpPut("enrolments/{id:int}/status")]
    [SwaggerOperation(Summary = "Update an enrolment status", Description = "Organiser only, for an enrolment in an event they own. Allowed status values are Pending, Confirmed, and Cancelled.")]
    [ProducesResponseType(typeof(EnrolmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Status(int id, UpdateEnrolmentStatusRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        if (!new[] { "Pending", "Confirmed", "Cancelled" }.Contains(r.Status, StringComparer.OrdinalIgnoreCase))
            return BadRequest();
        var x = await Db.Enrolments.Include(x => x.Event).SingleOrDefaultAsync(x => x.EnrolmentId == id);
        if (x is null)
            return NotFound();
        if (x.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden);
        x.Status = char.ToUpperInvariant(r.Status[0]) + r.Status[1..].ToLowerInvariant();
        await Db.SaveChangesAsync();
        return Ok(D(x));
    }
}

