using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Participants enrol in events; Organisers view enrolments for events they own.</summary>
[ApiController, Authorize, Route("api"), Produces("application/json")]
public class EnrolmentsController(AppDbContext db) : ControllerBase
{
    /// <summary>Enrols the current Participant in an event and selected category.</summary>
    [HttpPost("enrolments"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateEnrolment(CreateEnrolmentRequest request)
    {
        var e = await db.Events.FindAsync(request.EventId);
        if (e is null) return NotFound(new { message = "Event not found." });
        if (e.EventStatus != "Upcoming" || e.EventDate <= DateTime.UtcNow)
            return Conflict(new { message = "This event is no longer accepting enrolments." });
        var category = await db.Categories.SingleOrDefaultAsync(c => c.CategoryId == request.CategoryId && c.EventId == request.EventId);
        if (category is null) return BadRequest(new { message = "The selected category does not belong to this event." });
        var participantId = User.CurrentUserId();
        if (await db.Enrolments.AnyAsync(x => x.ParticipantId == participantId && x.EventId == request.EventId))
            return Conflict(new { message = "You are already enrolled in this event." });
        var enrolment = new Enrolment
        {
            ParticipantId = participantId, EventId = request.EventId, CategoryId = request.CategoryId,
            EnrolmentDate = DateTime.UtcNow, EnrolmentStatus = "Confirmed"
        };
        db.Enrolments.Add(enrolment);
        await db.SaveChangesAsync();
        await db.Entry(enrolment).Reference(x => x.Participant).LoadAsync();
        await db.Entry(enrolment).Reference(x => x.Event).LoadAsync();
        await db.Entry(enrolment).Reference(x => x.Category).LoadAsync();
        return CreatedAtAction(nameof(GetEnrolment), new { enrolmentId = enrolment.EnrolmentId }, enrolment.ToResponse());
    }

    /// <summary>Returns enrolments belonging to the current Participant.</summary>
    [HttpGet("enrolments/mine"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), 200)]
    public async Task<IActionResult> GetMyEnrolments()
    {
        var rows = await db.Enrolments.Include(x => x.Participant).Include(x => x.Event).Include(x => x.Category)
            .AsNoTracking().Where(x => x.ParticipantId == User.CurrentUserId()).OrderByDescending(x => x.EnrolmentDate).ToListAsync();
        return Ok(rows.Select(x => x.ToResponse()));
    }

    /// <summary>Returns all enrolments for an event owned by the current Organiser.</summary>
    [HttpGet("events/{eventId:int}/enrolments"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<EnrolmentResponse>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEventEnrolments(int eventId)
    {
        var e = await db.Events.FindAsync(eventId);
        if (e is null) return NotFound();
        if (e.OrganiserId != User.CurrentUserId()) return Forbid();
        var rows = await db.Enrolments.Include(x => x.Participant).Include(x => x.Event).Include(x => x.Category)
            .AsNoTracking().Where(x => x.EventId == eventId).OrderBy(x => x.EnrolmentDate).ToListAsync();
        return Ok(rows.Select(x => x.ToResponse()));
    }

    /// <summary>Returns an enrolment to its Participant or the Organiser who owns its event.</summary>
    [HttpGet("enrolments/{enrolmentId:int}")]
    [ProducesResponseType(typeof(EnrolmentResponse), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEnrolment(int enrolmentId)
    {
        var row = await db.Enrolments.Include(x => x.Participant).Include(x => x.Event).Include(x => x.Category)
            .AsNoTracking().SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId);
        if (row is null) return NotFound();
        var userId = User.CurrentUserId();
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        if (!((role == "Participant" && row.ParticipantId == userId) ||
              (role == "Organiser" && row.Event.OrganiserId == userId))) return Forbid();
        return Ok(row.ToResponse());
    }

    /// <summary>Cancels the current Participant's own enrolment without deleting its history.</summary>
    [HttpDelete("enrolments/{enrolmentId:int}"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(EnrolmentResponse), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CancelEnrolment(int enrolmentId)
    {
        var row = await db.Enrolments.Include(x => x.Participant).Include(x => x.Event).Include(x => x.Category)
            .SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId);
        if (row is null) return NotFound();
        if (row.ParticipantId != User.CurrentUserId()) return Forbid();
        if (row.Event.EventDate <= DateTime.UtcNow || row.Event.EventStatus == "Completed")
            return Conflict(new { message = "An enrolment cannot be cancelled after the event has started or completed." });
        row.EnrolmentStatus = "Cancelled";
        await db.SaveChangesAsync();
        return Ok(row.ToResponse());
    }
}
