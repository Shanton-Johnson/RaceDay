using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Browse events and allow Organisers to manage their own events.</summary>
[ApiController, Route("api/events"), Produces("application/json")]
public class EventsController(AppDbContext db) : ControllerBase
{
    /// <summary>Lists events. Optional filters: eventType (Run, Walk, Cycle), location, fromDate and toDate.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<EventResponse>), 200)]
    public async Task<IActionResult> GetEvents([FromQuery] string? eventType, [FromQuery] string? location,
        [FromQuery] DateTime? fromDate, [FromQuery] DateTime? toDate)
    {
        var query = db.Events.Include(e => e.EventType).AsNoTracking().AsQueryable();
        if (!string.IsNullOrWhiteSpace(eventType)) query = query.Where(e => e.EventType.TypeName == eventType);
        if (!string.IsNullOrWhiteSpace(location)) query = query.Where(e => e.Location.Contains(location));
        if (fromDate.HasValue) query = query.Where(e => e.EventDate >= fromDate.Value);
        if (toDate.HasValue) query = query.Where(e => e.EventDate <= toDate.Value);
        var events = await query.OrderBy(e => e.EventDate).ToListAsync();
        return Ok(events.Select(e => e.ToResponse()));
    }

    /// <summary>Returns the details of one event.</summary>
    [HttpGet("{eventId:int}")]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEventById(int eventId)
    {
        var e = await db.Events.Include(x => x.EventType).AsNoTracking().SingleOrDefaultAsync(x => x.EventId == eventId);
        return e is null ? NotFound(new { message = "Event not found." }) : Ok(e.ToResponse());
    }

    /// <summary>Creates an event owned by the logged-in Organiser.</summary>
    [HttpPost, Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    public async Task<IActionResult> CreateEvent(CreateEventRequest request)
    {
        var type = await db.EventTypes.FindAsync(request.EventTypeId);
        if (type is null) return BadRequest(new { message = "The selected event type does not exist." });
        var e = new Event
        {
            OrganiserId = User.CurrentUserId(), EventTypeId = request.EventTypeId,
            EventName = request.EventName.Trim(), Description = request.Description.Trim(),
            EventDate = request.EventDate, Location = request.Location.Trim(), DistanceKm = request.DistanceKm,
            BannerImageUrl = request.BannerImageUrl, EventStatus = "Upcoming", CreatedAt = DateTime.UtcNow
        };
        db.Events.Add(e);
        await db.SaveChangesAsync();
        e.EventType = type;
        return CreatedAtAction(nameof(GetEventById), new { eventId = e.EventId }, e.ToResponse());
    }

    /// <summary>Updates an event. Only the Organiser who owns the event can update it.</summary>
    [HttpPut("{eventId:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(EventResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateEvent(int eventId, UpdateEventRequest request)
    {
        var e = await db.Events.Include(x => x.EventType).SingleOrDefaultAsync(x => x.EventId == eventId);
        if (e is null) return NotFound(new { message = "Event not found." });
        if (e.OrganiserId != User.CurrentUserId()) return Forbid();
        if (request.EventStatus is not null && request.EventStatus is not ("Upcoming" or "Completed" or "Cancelled"))
            return BadRequest(new { message = "EventStatus must be Upcoming, Completed or Cancelled." });
        var type = await db.EventTypes.FindAsync(request.EventTypeId);
        if (type is null) return BadRequest(new { message = "The selected event type does not exist." });
        e.EventTypeId = request.EventTypeId;
        e.EventName = request.EventName.Trim(); e.Description = request.Description.Trim();
        e.EventDate = request.EventDate; e.Location = request.Location.Trim(); e.DistanceKm = request.DistanceKm;
        e.BannerImageUrl = request.BannerImageUrl;
        if (request.EventStatus is not null) e.EventStatus = request.EventStatus;
        await db.SaveChangesAsync();
        e.EventType = type;
        return Ok(e.ToResponse());
    }

    /// <summary>Deletes an event owned by the current Organiser when it has no dependent categories or enrolments.</summary>
    [HttpDelete("{eventId:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(204)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> DeleteEvent(int eventId)
    {
        var e = await db.Events.FindAsync(eventId);
        if (e is null) return NotFound();
        if (e.OrganiserId != User.CurrentUserId()) return Forbid();
        if (await db.Categories.AnyAsync(c => c.EventId == eventId) || await db.Enrolments.AnyAsync(x => x.EventId == eventId))
            return Conflict(new { message = "This event has categories or enrolments. Cancel it instead of deleting it." });
        db.Events.Remove(e);
        await db.SaveChangesAsync();
        return NoContent();
    }
}
