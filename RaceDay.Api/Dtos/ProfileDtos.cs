using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class UpdateProfileRequest
{
    [Required, StringLength(60, MinimumLength = 2)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(60, MinimumLength = 2)] public string LastName { get; set; } = string.Empty;
    [Phone, StringLength(25)] public string? PhoneNumber { get; set; }
    [Url, StringLength(2048)] public string? ProfilePictureUrl { get; set; }
}
