using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Swashbuckle.AspNetCore.Annotations;
using RaceDay.Api.Data;
using RaceDay.Api.Models;
using RaceDay.Contracts;

namespace RaceDay.Api.Controllers;

[Route("api")]
public class CategoriesController(RaceDayDbContext db) : ApiControllerBase(db)
{
    [HttpGet("events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "List the categories for an event", Description = "Requires a signed-in user. Returns category IDs for use when a Participant enrols.")]
    [ProducesResponseType(typeof(List<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetForEvent(int eventId)
    {
        if (RequireLogin() is IActionResult error) return error;
        if (!await Db.Events.AnyAsync(x => x.EventId == eventId)) return NotFound();
        return Ok(await Db.Categories.Where(x => x.EventId == eventId)
            .Select(x => new CategoryDto(x.CategoryId, x.EventId, x.Name, x.Distance, x.MinAge)).ToListAsync());
    }

    [HttpPost("events/{eventId:int}/categories")]
    [SwaggerOperation(Summary = "Create a category", Description = "Organiser only. The event must belong to the current Organiser. Supply a name, distance label, and non-negative minimum age.")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Create(int eventId, CreateCategoryRequest request)
    {
        if (RequireRole("Organiser") is IActionResult error) return error;
        var item = await Db.Events.FindAsync(eventId);
        if (item is null) return NotFound();
        if (item.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden);
        if (!ModelState.IsValid || request.MinAge < 0) return BadRequest();
        var category = new Category { EventId = eventId, Name = request.Name.Trim(), Distance = request.Distance.Trim(), MinAge = request.MinAge };
        Db.Categories.Add(category);
        await Db.SaveChangesAsync();
        return Created($"/api/categories/{category.CategoryId}", new CategoryDto(category.CategoryId, category.EventId, category.Name, category.Distance, category.MinAge));
    }

    [HttpPut("categories/{id:int}")]
    [SwaggerOperation(Summary = "Update an owned category", Description = "Organiser only. Replaces the category name, distance label, and minimum age. Categories with enrolments cannot be deleted.")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, CreateCategoryRequest request)
    {
        if (RequireRole("Organiser") is IActionResult error) return error;
        var category = await Db.Categories.Include(x => x.Event).SingleOrDefaultAsync(x => x.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden);
        if (!ModelState.IsValid || request.MinAge < 0) return BadRequest();
        category.Name = request.Name.Trim();
        category.Distance = request.Distance.Trim();
        category.MinAge = request.MinAge;
        await Db.SaveChangesAsync();
        return Ok(new CategoryDto(category.CategoryId, category.EventId, category.Name, category.Distance, category.MinAge));
    }

    [HttpDelete("categories/{id:int}")]
    [SwaggerOperation(Summary = "Delete an owned category", Description = "Organiser only. Returns 409 Conflict if any enrolment uses this category.")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id)
    {
        if (RequireRole("Organiser") is IActionResult error) return error;
        var category = await Db.Categories.Include(x => x.Event).SingleOrDefaultAsync(x => x.CategoryId == id);
        if (category is null) return NotFound();
        if (category.Event!.OrganiserId != CurrentUserId) return StatusCode(StatusCodes.Status403Forbidden);
        if (await Db.Enrolments.AnyAsync(enrolment => enrolment.CategoryId == id))
            return Conflict(new { message = "This category has enrolments and cannot be deleted." });
        Db.Categories.Remove(category);
        await Db.SaveChangesAsync();
        return Ok(new { message = "Category deleted." });
    }
}
