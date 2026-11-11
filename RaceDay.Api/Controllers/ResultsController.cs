using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Organisers capture results for their events; Participants view their own race history.</summary>
[ApiController, Authorize, Route("api"), Produces("application/json")]
public class ResultsController(AppDbContext db) : ControllerBase
{
    /// <summary>Records a finish time and position for an enrolment in an event owned by the Organiser.</summary>
    [HttpPost("enrolments/{enrolmentId:int}/result"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), 201)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    [ProducesResponseType(409)]
    public async Task<IActionResult> CreateResult(int enrolmentId, CreateResultRequest request)
    {
        var enrolment = await db.Enrolments.Include(x => x.Event).Include(x => x.Participant)
            .Include(x => x.Category).SingleOrDefaultAsync(x => x.EnrolmentId == enrolmentId);
        if (enrolment is null) return NotFound(new { message = "Enrolment not found." });
        if (enrolment.Event.OrganiserId != User.CurrentUserId()) return Forbid();
        if (enrolment.Event.EventDate > DateTime.UtcNow && enrolment.Event.EventStatus != "Completed")
            return Conflict(new { message = "Results can only be recorded after the event has concluded." });
        if (request.FinishTime <= TimeSpan.Zero || request.FinishingPosition > request.TotalFinishers)
            return BadRequest(new { message = "FinishTime must be positive and FinishingPosition cannot exceed TotalFinishers." });
        if (await db.Results.AnyAsync(r => r.EnrolmentId == enrolmentId))
            return Conflict(new { message = "A result already exists for this enrolment." });
        var result = new Result
        {
            EnrolmentId = enrolmentId, FinishTime = request.FinishTime,
            FinishingPosition = request.FinishingPosition, TotalFinishers = request.TotalFinishers,
            RecordedAt = DateTime.UtcNow
        };
        db.Results.Add(result);
        await db.SaveChangesAsync();
        result.Enrolment = enrolment;
        return CreatedAtAction(nameof(GetResult), new { resultId = result.ResultId }, result.ToResponse());
    }

    /// <summary>Returns the current Participant's personal results, including event and category details.</summary>
    [HttpGet("results/mine"), Authorize(Roles = "Participant")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), 200)]
    public async Task<IActionResult> GetMyResults()
    {
        var rows = await ResultsQuery().Where(r => r.Enrolment.ParticipantId == User.CurrentUserId())
            .OrderByDescending(r => r.Enrolment.Event.EventDate).ToListAsync();
        return Ok(rows.Select(r => r.ToResponse()));
    }

    /// <summary>Returns results for an event owned by the current Organiser.</summary>
    [HttpGet("events/{eventId:int}/results"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(IEnumerable<ResultResponse>), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetEventResults(int eventId)
    {
        var e = await db.Events.FindAsync(eventId);
        if (e is null) return NotFound();
        if (e.OrganiserId != User.CurrentUserId()) return Forbid();
        var rows = await ResultsQuery().Where(r => r.Enrolment.EventId == eventId)
            .OrderBy(r => r.FinishingPosition).ToListAsync();
        return Ok(rows.Select(r => r.ToResponse()));
    }

    /// <summary>Returns a result to its Participant or the Organiser who owns the event.</summary>
    [HttpGet("results/{resultId:int}")]
    [ProducesResponseType(typeof(ResultResponse), 200)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetResult(int resultId)
    {
        var result = await ResultsQuery().SingleOrDefaultAsync(r => r.ResultId == resultId);
        if (result is null) return NotFound();
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;
        var userId = User.CurrentUserId();
        if (!((role == "Participant" && result.Enrolment.ParticipantId == userId) ||
              (role == "Organiser" && result.Enrolment.Event.OrganiserId == userId))) return Forbid();
        return Ok(result.ToResponse());
    }

    /// <summary>Corrects a result for an event owned by the current Organiser.</summary>
    [HttpPut("results/{resultId:int}"), Authorize(Roles = "Organiser")]
    [ProducesResponseType(typeof(ResultResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(403)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> UpdateResult(int resultId, UpdateResultRequest request)
    {
        var result = await db.Results.Include(r => r.Enrolment).ThenInclude(e => e.Participant)
            .Include(r => r.Enrolment).ThenInclude(e => e.Event)
            .Include(r => r.Enrolment).ThenInclude(e => e.Category)
            .SingleOrDefaultAsync(r => r.ResultId == resultId);
        if (result is null) return NotFound();
        if (result.Enrolment.Event.OrganiserId != User.CurrentUserId()) return Forbid();
        if (request.FinishTime <= TimeSpan.Zero || request.FinishingPosition > request.TotalFinishers)
            return BadRequest(new { message = "FinishTime must be positive and FinishingPosition cannot exceed TotalFinishers." });
        result.FinishTime = request.FinishTime;
        result.FinishingPosition = request.FinishingPosition;
        result.TotalFinishers = request.TotalFinishers;
        await db.SaveChangesAsync();
        return Ok(result.ToResponse());
    }

    private IQueryable<Result> ResultsQuery() => db.Results
        .Include(r => r.Enrolment).ThenInclude(e => e.Participant)
        .Include(r => r.Enrolment).ThenInclude(e => e.Event)
        .Include(r => r.Enrolment).ThenInclude(e => e.Category)
        .AsNoTracking();
}
