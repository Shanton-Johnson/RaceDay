using System.ComponentModel.DataAnnotations;

namespace RaceDay.Api.Dtos;

public class RegisterRequest
{
    [Required, StringLength(60, MinimumLength = 2)] public string FirstName { get; set; } = string.Empty;
    [Required, StringLength(60, MinimumLength = 2)] public string LastName { get; set; } = string.Empty;
    [Required, EmailAddress, StringLength(254)] public string Email { get; set; } = string.Empty;
    [Required, MinLength(10), StringLength(100)] public string Password { get; set; } = string.Empty;
    [Phone, StringLength(25)] public string? PhoneNumber { get; set; }
    [Required] public string Role { get; set; } = "Participant";
}

public class LoginRequest
{
    [Required, EmailAddress] public string Email { get; set; } = string.Empty;
    [Required] public string Password { get; set; } = string.Empty;
}

public record UserResponse(int UserId, string FirstName, string LastName, string Email, string Role, string? PhoneNumber, string? ProfilePictureUrl);
