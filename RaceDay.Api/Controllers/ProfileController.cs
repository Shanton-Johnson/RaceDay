using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDay.Api.Data;
using RaceDay.Api.Dtos;

namespace RaceDay.Api.Controllers;

/// <summary>View and update the currently authenticated user's profile.</summary>
[ApiController, Authorize, Route("api/profile"), Produces("application/json")]
public class ProfileController(AppDbContext db) : ControllerBase
{
    /// <summary>Returns the current user's profile, never the password hash.</summary>
    [HttpGet]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> GetProfile()
    {
        var user = await db.AppUsers.FindAsync(User.CurrentUserId());
        return user is null ? NotFound() : Ok(user.ToResponse());
    }

    /// <summary>Updates the current user's own editable profile fields. The role and email cannot be changed here.</summary>
    [HttpPut]
    [ProducesResponseType(typeof(UserResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(401)]
    public async Task<IActionResult> UpdateProfile(UpdateProfileRequest request)
    {
        var user = await db.AppUsers.FindAsync(User.CurrentUserId());
        if (user is null) return NotFound();
        user.FirstName = request.FirstName.Trim();
        user.LastName = request.LastName.Trim();
        user.PhoneNumber = request.PhoneNumber?.Trim();
        user.ProfilePictureUrl = request.ProfilePictureUrl;
        await db.SaveChangesAsync();
        return Ok(user.ToResponse());
    }
}
