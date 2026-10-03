using PatriotAlarm.Api.Domain;

namespace PatriotAlarm.Api.Controllers;

public record AlarmEventDto(
    int Id,
    string AccountNumber,
    string? SiteName,
    DateTime ReceivedAtUtc,
    string EventCode,
    string Description,
    string Zone,
    string Group,
    AlarmPriority Priority,
    AlarmStatus Status,
    int? JobId,
    string? OfficerName,
    JobStatus? JobStatus);

public record JobDto(
    int Id,
    int AlarmEventId,
    string AlarmDescription,
    string? SiteName,
    string OfficerName,
    JobStatus Status,
    DateTime DispatchedAtUtc,
    DateTime? EnRouteAtUtc,
    DateTime? OnSiteAtUtc,
    DateTime? CompletedAtUtc,
    string? Notes);

public record SiteDto(int Id, string AccountNumber, string Name, string? Address);

public record AuditDto(int Id, DateTime AtUtc, string Actor, string Action, string Details);

public record AcknowledgeRequest(string Actor);

public record DispatchRequest(string Actor, string OfficerName);

public record ClearRequest(string Actor);

public record AdvanceJobRequest(string Actor, JobStatus Status, string? Notes);

public record CreateSiteRequest(string AccountNumber, string Name, string? Address);
