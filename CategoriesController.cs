using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/events/{eventId}/categories")]
[Authorize]
public class CategoriesController : ControllerBase
{
    private readonly AppDbContext _db;
    public CategoriesController(AppDbContext db) { _db = db; }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>View the categories of an event. Both roles can use this.</summary>
    [HttpGet]
    public async Task<IActionResult> Get(int eventId) =>
        Ok(await _db.Categories.Where(c => c.EventId == eventId).ToListAsync());

    /// <summary>Add a category (e.g. Under 20, Senior, 10km) to your event. Organisers only.</summary>
    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(int eventId, CategoryDto dto)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound("Event not found.");
        if (ev.OrganiserId != UserId) return Forbid();

        var c = new Category { Name = dto.Name, EventId = eventId };
        _db.Categories.Add(c);
        await _db.SaveChangesAsync();
        return Ok(c);
    }
}