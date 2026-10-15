using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api")]
[Authorize]
public class EnrolmentsController : ControllerBase
{
    private readonly AppDbContext _db;
    public EnrolmentsController(AppDbContext db) { _db = db; }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

   [HttpPost("enrolments")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Enrol(EnrolDto dto)
    {
        var category = await _db.Categories.FirstOrDefaultAsync(c => c.Id == dto.CategoryId && c.EventId == dto.EventId);
        if (category == null) return BadRequest("That category does not belong to that event.");

        if (await _db.Enrolments.AnyAsync(e => e.EventId == dto.EventId && e.ParticipantId == UserId))
            return Conflict("You are already enrolled in this event.");

        var enrolment = new Enrolment { EventId = dto.EventId, CategoryId = dto.CategoryId, ParticipantId = UserId };
        _db.Enrolments.Add(enrolment);
        await _db.SaveChangesAsync();
        return Ok(enrolment);
    }

   [HttpGet("enrolments/mine")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine() =>
        Ok(await _db.Enrolments.Where(e => e.ParticipantId == UserId).ToListAsync());

    [HttpGet("events/{eventId}/enrolments")]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> ForEvent(int eventId)
    {
        var ev = await _db.Events.FindAsync(eventId);
        if (ev == null) return NotFound();
        if (ev.OrganiserId != UserId) return Forbid();

        var list = await _db.Enrolments
            .Where(e => e.EventId == eventId)
            .Select(e => new { e.Id, e.CategoryId, e.ParticipantId })
            .ToListAsync();
        return Ok(list);
    }
}