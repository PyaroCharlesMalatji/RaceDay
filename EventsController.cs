using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/events")]
[Authorize]   // everything needs login by default
public class EventsController : ControllerBase
{
    private readonly AppDbContext _db;
    public EventsController(AppDbContext db) { _db = db; }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
 
    [HttpGet]
    public async Task<IActionResult> GetAll() => Ok(await _db.Events.ToListAsync());

    [HttpGet("{id}")]
    public async Task<IActionResult> Get(int id)
    {
        var e = await _db.Events.FindAsync(id);
        return e == null ? NotFound() : Ok(e);
    }

     [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(EventDto dto)
    {
        var e = new Event
        {
            Name = dto.Name,
            Description = dto.Description,
            Date = dto.Date,
            Location = dto.Location,
            DistanceKm = dto.DistanceKm,
            EventType = dto.EventType,
            OrganiserId = UserId
        };
        _db.Events.Add(e);
        await _db.SaveChangesAsync();
        return Ok(e);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Update(int id, EventDto dto)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != UserId) return Forbid();   // can't edit someone else's event

        e.Name = dto.Name; e.Description = dto.Description; e.Date = dto.Date;
        e.Location = dto.Location; e.DistanceKm = dto.DistanceKm; e.EventType = dto.EventType;
        await _db.SaveChangesAsync();
        return Ok(e);
    }

 [HttpDelete("{id}")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Delete(int id)
    {
        var e = await _db.Events.FindAsync(id);
        if (e == null) return NotFound();
        if (e.OrganiserId != UserId) return Forbid();

        _db.Events.Remove(e);
        await _db.SaveChangesAsync();
        return Ok("Event deleted.");
    }
}