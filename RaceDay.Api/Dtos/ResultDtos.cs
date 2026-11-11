using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CreateResultRequest
{
    public TimeSpan FinishTime { get; set; }
    [Range(1, int.MaxValue)] public int FinishingPosition { get; set; }
    [Range(1, int.MaxValue)] public int TotalFinishers { get; set; }
}

public class UpdateResultRequest : CreateResultRequest { }

public record ResultResponse(int ResultId, int EnrolmentId, int ParticipantId, string ParticipantName,
    int EventId, string EventName, DateTime EventDate, string CategoryName, TimeSpan FinishTime,
    int FinishingPosition, int TotalFinishers, DateTime RecordedAt);
