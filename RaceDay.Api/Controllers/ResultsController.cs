using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;
namespace RaceDay.Api.Controllers;

[Route("api")]
public class ResultsController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpPost("enrolments/{enrolmentId:int}/results")]
    [SwaggerOperation(Summary = "Record a race result", Description = "Organiser only, for an enrolment in an event they own. The route and body enrolment IDs must match. Enter finish time in minutes when available; status must be Completed, DNF, DNS, or Disqualified. Only one result is allowed per enrolment.")]
    [ProducesResponseType(typeof(ResultDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(int enrolmentId, CreateResultRequest r)
    {
        if (RequireRole("Organiser") is IActionResult e)
            return e;
        if (enrolmentId != r.EnrolmentId)
            return BadRequest();
        var en = await Db.Enrolments.Include(x => x.Event).SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId);
        if (en is null)
            return NotFound();
        if (en.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden);
        if (r.FinishTime < 0 || !new[] { "Completed", "DNF", "DNS", "Disqualified" }.Contains(r.ResultStatus, StringComparer.OrdinalIgnoreCase))
            return BadRequest();
        if (await Db.Results.AnyAsync(x => x.EnrolmentId == enrolmentId))
            return Conflict(new
            {
                message = "A result has already been recorded for this enrolment."
            });
        var x = new Result { EnrolmentId = enrolmentId, FinishTime = r.FinishTime, Position = r.Position, ResultStatus = r.ResultStatus };
        Db.Results.Add(x);
        await Db.SaveChangesAsync();
        return Created("/api/user/results", new ResultDto(x.ResultId, x.EnrolmentId, x.FinishTime, x.Position, x.ResultStatus));
    }
    [HttpGet("user/results")]
    [SwaggerOperation(Summary = "List your results", Description = "Participant only. Returns results linked to the signed-in Participant's enrolments.")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Mine()
    {
        if (RequireRole("Participant") is IActionResult e)
            return e;
        return Ok(await Db.Results.Where(x => x.Enrolment!.UserId == CurrentUserId).Select(x => new ResultDto(x.ResultId, x.EnrolmentId, x.FinishTime, x.Position, x.ResultStatus)).ToListAsync());
    }
    [HttpGet("events/{eventId:int}/results")]
    [SwaggerOperation(Summary = "List results for an owned event", Description = "Organiser only. Returns results for enrolments in an event owned by the signed-in Organiser.")]
    [ProducesResponseType(typeof(List<ResultDto>), StatusCodes.Status200OK)]
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
        return Ok(await Db.Results.Where(x => x.Enrolment!.EventId == eventId).Select(x => new ResultDto(x.ResultId, x.EnrolmentId, x.FinishTime, x.Position, x.ResultStatus)).ToListAsync());
    }
}

