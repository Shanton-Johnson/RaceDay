using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CreateEventRequest
{
    [Range(1, int.MaxValue)] public int EventTypeId { get; set; }
    [Required, StringLength(150)] public string EventName { get; set; } = string.Empty;
    [Required, StringLength(2000)] public string Description { get; set; } = string.Empty;
    public DateTime EventDate { get; set; }
    [Required, StringLength(200)] public string Location { get; set; } = string.Empty;
    [Range(typeof(decimal), "0.01", "999999.99")] public decimal DistanceKm { get; set; }
    [Url, StringLength(2048)] public string? BannerImageUrl { get; set; }
}

public class UpdateEventRequest : CreateEventRequest
{
    public string? EventStatus { get; set; }
}

public record EventResponse(int EventId, int OrganiserId, string EventName, string Description, DateTime EventDate,
    string Location, decimal DistanceKm, int EventTypeId, string EventType, string? BannerImageUrl, string EventStatus);
