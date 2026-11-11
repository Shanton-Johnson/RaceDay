using System.Security.Claims;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

internal static class ApiMapping
{
    public static int CurrentUserId(this ClaimsPrincipal user) =>
        int.Parse(user.FindFirstValue(ClaimTypes.NameIdentifier)!);

    public static UserResponse ToResponse(this AppUser user) =>
        new(user.UserId, user.FirstName, user.LastName, user.Email, user.Role, user.PhoneNumber, user.ProfilePictureUrl);

    public static EventResponse ToResponse(this Event e) =>
        new(e.EventId, e.OrganiserId, e.EventName, e.Description, e.EventDate, e.Location,
            e.DistanceKm, e.EventTypeId, e.EventType.TypeName, e.BannerImageUrl, e.EventStatus);

    public static CategoryResponse ToResponse(this Category c) =>
        new(c.CategoryId, c.EventId, c.CategoryName, c.CategoryType, c.MinAge, c.MaxAge, c.MinDistanceKm, c.MaxDistanceKm);

    public static EnrolmentResponse ToResponse(this Enrolment e) =>
        new(e.EnrolmentId, e.ParticipantId, $"{e.Participant.FirstName} {e.Participant.LastName}",
            e.EventId, e.Event.EventName, e.CategoryId, e.Category.CategoryName, e.EnrolmentDate, e.EnrolmentStatus);

    public static ResultResponse ToResponse(this Result r) =>
        new(r.ResultId, r.EnrolmentId, r.Enrolment.ParticipantId,
            $"{r.Enrolment.Participant.FirstName} {r.Enrolment.Participant.LastName}",
            r.Enrolment.EventId, r.Enrolment.Event.EventName, r.Enrolment.Event.EventDate,
            r.Enrolment.Category.CategoryName, r.FinishTime, r.FinishingPosition, r.TotalFinishers, r.RecordedAt);
}
