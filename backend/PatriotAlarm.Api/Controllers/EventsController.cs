using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PatriotAlarm.Api.Data;
using PatriotAlarm.Api.Domain;
using PatriotAlarm.Api.Services;

namespace PatriotAlarm.Api.Controllers;

[ApiController]
[Route("api/events")]
public class EventsController(AppDbContext db, AlarmService alarms) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IEnumerable<AlarmEventDto>>> List([FromQuery] AlarmStatus? status, CancellationToken ct)
    {
        var query = db.AlarmEvents
            .Include(a => a.Site)
            .Include(a => a.Job)
            .AsQueryable();

        if (status is not null)
        {
            query = query.Where(a => a.Status == status);
        }

        var items = await query
            .OrderByDescending(a => a.ReceivedAtUtc)
            .Take(200)
            .Select(a => new AlarmEventDto(
                a.Id,
                a.AccountNumber,
                a.Site != null ? a.Site.Name : null,
                a.ReceivedAtUtc,
                a.EventCode,
                a.Description,
                a.Zone,
                a.Group,
                a.Priority,
                a.Status,
                a.Job != null ? a.Job.Id : null,
                a.Job != null ? a.Job.OfficerName : null,
                a.Job != null ? a.Job.Status : null))
            .ToListAsync(ct);

        return Ok(items);
    }

    [HttpPost("{id:int}/acknowledge")]
    public async Task<IActionResult> Acknowledge(int id, [FromBody] AcknowledgeRequest request, CancellationToken ct)
    {
        await alarms.AcknowledgeAsync(id, request.Actor, ct);
        return NoContent();
    }

    [HttpPost("{id:int}/dispatch")]
    public async Task<IActionResult> Dispatch(int id, [FromBody] DispatchRequest request, CancellationToken ct)
    {
        var job = await alarms.DispatchAsync(id, request.OfficerName, request.Actor, ct);
        return CreatedAtRoute("GetJob", new { id = job.Id }, new { job.Id, job.Status });
    }

    [HttpPost("{id:int}/clear")]
    public async Task<IActionResult> Clear(int id, [FromBody] ClearRequest request, CancellationToken ct)
    {
        await alarms.ClearAsync(id, request.Actor, ct);
        return NoContent();
    }
}
