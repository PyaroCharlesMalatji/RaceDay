using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RaceDayApi.Data;
using RaceDayApi.Models;

namespace RaceDayApi.Controllers;

[ApiController]
[Route("api/results")]
[Authorize]
public class ResultsController : ControllerBase
{
    private readonly AppDbContext _db;
    public ResultsController(AppDbContext db) { _db = db; }

    private int UserId => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpPost]
    [Authorize(Roles = "Organiser")]
    public async Task<IActionResult> Create(ResultDto dto)
    {
        var enrolment = await _db.Enrolments.Include(e => e.Event).FirstOrDefaultAsync(e => e.Id == dto.EnrolmentId);
        if (enrolment == null) return NotFound("Enrolment not found.");
        if (enrolment.Event!.OrganiserId != UserId) return Forbid();   // only your own events

        if (!TimeSpan.TryParse(dto.FinishTime, out var time))
            return BadRequest("Finish time must look like 00:45:30.");

        var result = new Result { EnrolmentId = dto.EnrolmentId, FinishTime = time, Position = dto.Position };
        _db.Results.Add(result);
        await _db.SaveChangesAsync();
        return Ok(new { result.Id, result.EnrolmentId, FinishTime = dto.FinishTime, result.Position });
    }

    /// <summary>View your own results. Participants only.</summary>
    [HttpGet("mine")]
    [Authorize(Roles = "Participant")]
    public async Task<IActionResult> Mine()
    {
        var list = await _db.Results
            .Where(r => r.Enrolment!.ParticipantId == UserId)
            .Select(r => new { r.Id, r.EnrolmentId, r.Enrolment!.EventId, r.FinishTime, r.Position })
            .ToListAsync();
        return Ok(list);
    }
}