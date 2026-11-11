using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>View event categories and allow event-owning Organisers to manage them.</summary>
[ApiController, Route("api"), Produces("application/json")]
public class CategoriesController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists categories available for an event.</summary>
    [HttpGet("events/{eventId:int}/categories")]
    [ProducesResponseType(typeof(IEnumerable<CategoryResponse>), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetCategories(int eventId)
    {
        if (!await db.Events.AnyAsync(e => e.EventId == eventId)) return NotFound(new { message = "Event not found." });
        var categories = await db.Categories.AsNoTracking().Where(c => c.EventId == eventId).OrderBy(c => c.CategoryName).ToListAsync();
        return Ok(categories.Select(c => c.ToResponse()));
    }

    /// <summary>Adds an age or distance category to an event owned by the current Organiser.</summary>
    [HttpPost("events/{eventId:int}/categories"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateCategory(int eventId, CreateCategoryRequest request)
    {
        var e = await db.Events.FindAsync(eventId);
        if (e is null) return NotFound(new { message = "Event not found." });
        if (e.OrganiserId != User.CurrentUserId()) return Forbid();
        var error = ValidateCategory(request);
        if (error is not null) return BadRequest(new { message = error });
        if (await db.Categories.AnyAsync(c => c.EventId == eventId && c.CategoryName == request.CategoryName.Trim()))
            return Conflict(new { message = "A category with this name already exists for this event." });
        var category = FromRequest(eventId, request);
        db.Categories.Add(category);
        await db.SaveChangesAsync();
        return Created($"/api/events/{eventId}/categories", category.ToResponse());
    }

    /// <summary>Updates a category when its parent event belongs to the current Organiser.</summary>
    [HttpPut("categories/{categoryId:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(CategoryResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> UpdateCategory(int categoryId, UpdateCategoryRequest request)
    {
        var category = await db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != User.CurrentUserId()) return Forbid();
        var error = ValidateCategory(request);
        if (error is not null) return BadRequest(new { message = error });
        if (await db.Categories.AnyAsync(c => c.EventId == category.EventId && c.CategoryId != categoryId && c.CategoryName == request.CategoryName.Trim()))
            return Conflict(new { message = "A category with this name already exists for this event." });
        if (await db.Enrolments.AnyAsync(x => x.CategoryId == categoryId) &&
            (category.CategoryType != request.CategoryType || category.MinAge != request.MinAge || category.MaxAge != request.MaxAge ||
             category.MinDistanceKm != request.MinDistanceKm || category.MaxDistanceKm != request.MaxDistanceKm))
            return Conflict(new { message = "The category range cannot be changed after participants have enrolled." });
        category.CategoryName = request.CategoryName.Trim();
        category.CategoryType = request.CategoryType;
        category.MinAge = request.MinAge; category.MaxAge = request.MaxAge;
        category.MinDistanceKm = request.MinDistanceKm; category.MaxDistanceKm = request.MaxDistanceKm;
        await db.SaveChangesAsync();
        return Ok(category.ToResponse());
    }

    /// <summary>Deletes a category when no enrolments reference it.</summary>
    [HttpDelete("categories/{categoryId:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> DeleteCategory(int categoryId)
    {
        var category = await db.Categories.Include(c => c.Event).SingleOrDefaultAsync(c => c.CategoryId == categoryId);
        if (category is null) return NotFound();
        if (category.Event.OrganiserId != User.CurrentUserId()) return Forbid();
        if (await db.Enrolments.AnyAsync(e => e.CategoryId == categoryId))
            return Conflict(new { message = "This category has enrolments and cannot be deleted." });
        db.Categories.Remove(category);
        await db.SaveChangesAsync();
        return NoContent();
    }

    private static Category FromRequest(int eventId, CreateCategoryRequest request) => new()
    {
        EventId = eventId, CategoryName = request.CategoryName.Trim(), CategoryType = request.CategoryType,
        MinAge = request.MinAge, MaxAge = request.MaxAge,
        MinDistanceKm = request.MinDistanceKm, MaxDistanceKm = request.MaxDistanceKm
    };

    private static string? ValidateCategory(CreateCategoryRequest r)
    {
        if (r.CategoryType == "Age")
        {
            if (r.MinAge is null && r.MaxAge is null) return "An Age category needs a minimum age, maximum age, or both.";
            if (r.MinAge < 0 || r.MaxAge < 0 || (r.MinAge.HasValue && r.MaxAge.HasValue && r.MinAge > r.MaxAge))
                return "Age range is invalid.";
            if (r.MinDistanceKm is not null || r.MaxDistanceKm is not null) return "Age categories cannot contain distance values.";
            return null;
        }
        if (r.CategoryType == "Distance")
        {
            if (r.MinDistanceKm is null && r.MaxDistanceKm is null) return "A Distance category needs a minimum distance, maximum distance, or both.";
            if (r.MinDistanceKm <= 0 || r.MaxDistanceKm <= 0 ||
                (r.MinDistanceKm.HasValue && r.MaxDistanceKm.HasValue && r.MinDistanceKm > r.MaxDistanceKm))
                return "Distance range is invalid.";
            if (r.MinAge is not null || r.MaxAge is not null) return "Distance categories cannot contain age values.";
            return null;
        }
        return "CategoryType must be Age or Distance.";
    }
}
