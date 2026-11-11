using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;
using RaceDay.Api.Models;

namespace RaceDay.Api.Controllers;

/// <summary>Register, log in and log out RaceDay users. Passwords are always hashed.</summary>
[ApiController]
[Route("api/auth")]
[Produces("application/json")]
public class AuthController(AppDbContext db, IPasswordHasher<AppUser> hasher) : ControllerBase
{
    /// <summary>Creates an Organiser or Participant account.</summary>
    /// <remarks>Supply personal details, a unique email address, a password of at least 10 characters, and the role Organiser or Participant.</remarks>
    [HttpPost("register")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register(RegisterRequest request)
    {
        var role = request.Role.Trim();
        if (role is not ("Organiser" or "Participant"))
            return BadRequest(new { message = "Role must be Organiser or Participant." });

        var email = request.Email.Trim().ToLowerInvariant();
        if (await db.AppUsers.AnyAsync(u => u.Email == email))
            return Conflict(new { message = "An account with this email already exists." });

        var user = new AppUser
        {
            FirstName = request.FirstName.Trim(), LastName = request.LastName.Trim(),
            Email = email, PhoneNumber = request.PhoneNumber?.Trim(), Role = role,
            CreatedAt = DateTime.UtcNow
        };
        user.PasswordHash = hasher.HashPassword(user, request.Password);
        db.AppUsers.Add(user);
        await db.SaveChangesAsync();
        return Created("/api/profile", user.ToResponse());
    }

    /// <summary>Authenticates a user and stores their user ID and role in a server-side session.</summary>
    /// <remarks>After login, send the session cookie with subsequent protected requests. Swagger UI in the same browser retains the cookie.</remarks>
    [HttpPost("login")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var user = await db.AppUsers.SingleOrDefaultAsync(u => u.Email == email);
        if (user is null || hasher.VerifyHashedPassword(user, user.PasswordHash, request.Password) == PasswordVerificationResult.Failed)
            return Unauthorized(new { message = "Email or password is incorrect." });

        HttpContext.Session.SetInt32("UserId", user.UserId);
        HttpContext.Session.SetString("Role", user.Role);
        HttpContext.Session.SetString("Email", user.Email);
        return Ok(user.ToResponse());
    }

    /// <summary>Clears the current authenticated session.</summary>
    [HttpPost("logout")]
    [Microsoft.AspNetCore.Authorization.Authorize]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public IActionResult Logout()
    {
        HttpContext.Session.Clear();
        return NoContent();
    }
}
