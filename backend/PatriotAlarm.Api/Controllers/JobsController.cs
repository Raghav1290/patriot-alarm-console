using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatriotAlarm.Api.Data;
using PatriotAlarm.Api.Domain;
using PatriotAlarm.Api.Services;

namespace PatriotAlarm.Api.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(AppDbContext db, AlarmService alarms) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<JobDto>>> List([FromQuery] bool openOnly, CancellationToken ct)
    {
        var query = db.Jobs
            .Include(j => j.AlarmEvent).ThenInclude(a => a!.Site)
            .AsQueryable();

        if (openOnly)
        {
            query = query.Where(j => j.Status != JobStatus.Completed);
        }

        var items = await query
            .OrderByDescending(j => j.DispatchedAtUtc)
            .Select(j => new JobDto(
                j.Id,
                j.AlarmEventId,
                j.AlarmEvent!.Description,
                j.AlarmEvent.Site != null ? j.AlarmEvent.Site.Name : null,
                j.OfficerName,
                j.Status,
                j.DispatchedAtUtc,
                j.EnRouteAtUtc,
                j.OnSiteAtUtc,
                j.CompletedAtUtc,
                j.Notes))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpGet("{id:int}", Name = "GetJob")]
    public async Task<ActionResult<JobDto>> Get(int id, CancellationToken ct)
    {
        var job = await db.Jobs
            .Include(j => j.AlarmEvent).ThenInclude(a => a!.Site)
            .FirstOrDefaultAsync(j => j.Id == id, ct);

        if (job is null)
        {
            return NotFound();
        }

        return Ok(new JobDto(
            job.Id,
            job.AlarmEventId,
            job.AlarmEvent!.Description,
            job.AlarmEvent.Site?.Name,
            job.OfficerName,
            job.Status,
            job.DispatchedAtUtc,
            job.EnRouteAtUtc,
            job.OnSiteAtUtc,
            job.CompletedAtUtc,
            job.Notes));
    }

    [HttpPost("{id:int}/status")]
    public async Task<IActionResult> AdvanceStatus(int id, [FromBody] AdvanceJobRequest request, CancellationToken ct)
    {
        await alarms.AdvanceJobAsync(id, request.Status, request.Actor, request.Notes, ct);
        return NoContent();
    }
}
