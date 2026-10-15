using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/profile")]
[Authorize]   // any logged-in user (both roles)
public class ProfileController : ControllerBase
{
    private readonly AppDbContext _db;
    public ProfileController(AppDbContext db) { _db = db; }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>View your own profile.</summary>
    [HttpGet]
    public async Task<IActionResult> Get()
    {
        var u = await _db.Users.FindAsync(UserId);
        if (u == null) return NotFound();
        return Ok(new { u.Id, u.FirstName, u.LastName, u.Email, u.Role });
    }

    /// <summary>Update your first and last name.</summary>
    [HttpPut]
    public async Task<IActionResult> Update(ProfileDto dto)
    {
        var u = await _db.Users.FindAsync(UserId);
        if (u == null) return NotFound();
        u.FirstName = dto.FirstName;
        u.LastName = dto.LastName;
        await _db.SaveChangesAsync();
        return Ok("Profile updated.");
    }
}