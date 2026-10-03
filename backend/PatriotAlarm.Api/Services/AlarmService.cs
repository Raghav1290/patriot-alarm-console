using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using PatriotAlarm.Api.Data;
using PatriotAlarm.Api.Domain;
using PatriotAlarm.Api.Hubs;
using PatriotAlarm.Api.Receiver;

namespace PatriotAlarm.Api.Services;

/// <summary>
/// Holds the alarm and dispatch rules. Every state change writes an audit entry
/// and pushes the change to connected operator consoles.
/// </summary>
public class AlarmService(AppDbContext db, IHubContext<AlarmHub> hub, TimeProvider clock)
{
    private static readonly Dictionary<JobStatus, JobStatus?> NextStatus = new()
    {
        [JobStatus.Dispatched] = JobStatus.EnRoute,
        [JobStatus.EnRoute] = JobStatus.OnSite,
        [JobStatus.OnSite] = JobStatus.Completed,
        [JobStatus.Completed] = null,
    };

    public async Task<AlarmEvent> IngestAsync(ParsedAlarm parsed, string rawMessage, CancellationToken ct = default)
    {
        var site = await db.Sites.FirstOrDefaultAsync(s => s.AccountNumber == parsed.AccountNumber, ct);

        var alarm = new AlarmEvent
        {
            SiteId = site?.Id,
            AccountNumber = parsed.AccountNumber,
            ReceivedAtUtc = clock.GetUtcNow().UtcDateTime,
            EventCode = parsed.EventCode,
            Description = parsed.Description,
            Qualifier = parsed.Qualifier,
            Zone = parsed.Zone,
            Group = parsed.Group,
            Priority = parsed.Priority,
            RawMessage = rawMessage,
        };

        db.AlarmEvents.Add(alarm);
        AddAudit("Receiver", "AlarmReceived", $"{parsed.Description} on account {parsed.AccountNumber}, zone {parsed.Zone}");
        await db.SaveChangesAsync(ct);

        await hub.Clients.All.SendAsync(AlarmHub.EventChanged, alarm.Id, ct);
        return alarm;
    }

    public async Task<AlarmEvent> AcknowledgeAsync(int alarmId, string actor, CancellationToken ct = default)
    {
        var alarm = await GetAlarmAsync(alarmId, ct);
        if (alarm.Status != AlarmStatus.New)
        {
            throw new InvalidOperationException($"Only new alarms can be acknowledged. Current status: {alarm.Status}.");
        }

        alarm.Status = AlarmStatus.Acknowledged;
        AddAudit(actor, "AlarmAcknowledged", $"Alarm {alarm.Id} ({alarm.Description})");
        await db.SaveChangesAsync(ct);
        await hub.Clients.All.SendAsync(AlarmHub.EventChanged, alarm.Id, ct);
        return alarm;
    }

    public async Task<Job> DispatchAsync(int alarmId, string officerName, string actor, CancellationToken ct = default)
    {
        var alarm = await GetAlarmAsync(alarmId, ct);
        if (alarm.Status == AlarmStatus.Cleared)
        {
            throw new InvalidOperationException("Cleared alarms cannot be dispatched.");
        }
        if (alarm.Job is not null)
        {
            throw new InvalidOperationException($"Alarm {alarmId} already has a job.");
        }

        var job = new Job
        {
            AlarmEventId = alarm.Id,
            OfficerName = officerName,
            DispatchedAtUtc = clock.GetUtcNow().UtcDateTime,
        };

        alarm.Status = AlarmStatus.Dispatched;
        db.Jobs.Add(job);
        AddAudit(actor, "JobDispatched", $"Alarm {alarm.Id} dispatched to {officerName}");
        await db.SaveChangesAsync(ct);

        await hub.Clients.All.SendAsync(AlarmHub.EventChanged, alarm.Id, ct);
        await hub.Clients.All.SendAsync(AlarmHub.JobChanged, job.Id, ct);
        return job;
    }

    public async Task<AlarmEvent> ClearAsync(int alarmId, string actor, CancellationToken ct = default)
    {
        var alarm = await GetAlarmAsync(alarmId, ct);
        if (alarm.Job is { Status: not JobStatus.Completed })
        {
            throw new InvalidOperationException("Cannot clear an alarm while its job is still open.");
        }

        alarm.Status = AlarmStatus.Cleared;
        AddAudit(actor, "AlarmCleared", $"Alarm {alarm.Id} ({alarm.Description})");
        await db.SaveChangesAsync(ct);
        await hub.Clients.All.SendAsync(AlarmHub.EventChanged, alarm.Id, ct);
        return alarm;
    }

    public async Task<Job> AdvanceJobAsync(int jobId, JobStatus target, string actor, string? notes, CancellationToken ct = default)
    {
        var job = await db.Jobs.FirstOrDefaultAsync(j => j.Id == jobId, ct)
            ?? throw new KeyNotFoundException($"Job {jobId} not found.");

        if (NextStatus[job.Status] != target)
        {
            throw new InvalidOperationException($"Job cannot move from {job.Status} to {target}.");
        }

        var now = clock.GetUtcNow().UtcDateTime;
        job.Status = target;
        switch (target)
        {
            case JobStatus.EnRoute: job.EnRouteAtUtc = now; break;
            case JobStatus.OnSite: job.OnSiteAtUtc = now; break;
            case JobStatus.Completed: job.CompletedAtUtc = now; break;
        }
        if (!string.IsNullOrWhiteSpace(notes))
        {
            job.Notes = notes;
        }

        AddAudit(actor, "JobStatusChanged", $"Job {job.Id} is now {target}");
        await db.SaveChangesAsync(ct);
        await hub.Clients.All.SendAsync(AlarmHub.JobChanged, job.Id, ct);
        return job;
    }

    private async Task<AlarmEvent> GetAlarmAsync(int alarmId, CancellationToken ct)
    {
        return await db.AlarmEvents.Include(a => a.Job).FirstOrDefaultAsync(a => a.Id == alarmId, ct)
            ?? throw new KeyNotFoundException($"Alarm {alarmId} not found.");
    }

    private void AddAudit(string actor, string action, string details)
    {
        db.AuditEntries.Add(new AuditEntry
        {
            AtUtc = clock.GetUtcNow().UtcDateTime,
            Actor = actor,
            Action = action,
            Details = details,
        });
    }
}
