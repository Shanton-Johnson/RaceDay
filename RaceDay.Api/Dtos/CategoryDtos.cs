using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class CreateCategoryRequest
{
    [Required, StringLength(80)] public string CategoryName { get; set; } = string.Empty;
    [Required] public string CategoryType { get; set; } = string.Empty;
    public int? MinAge { get; set; }
    public int? MaxAge { get; set; }
    public decimal? MinDistanceKm { get; set; }
    public decimal? MaxDistanceKm { get; set; }
}

public class UpdateCategoryRequest : CreateCategoryRequest { }

public record CategoryResponse(int CategoryId, int EventId, string CategoryName, string CategoryType,
    int? MinAge, int? MaxAge, decimal? MinDistanceKm, decimal? MaxDistanceKm);
