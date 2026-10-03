namespace PatriotAlarm.Api.Domain;

public class Site
{
    public int Id { get; set; }
    public required string AccountNumber { get; set; }
    public required string Name { get; set; }
    public string? Address { get; set; }
}

public enum AlarmPriority { Info, Low, Medium, High, Critical }

public enum AlarmStatus { New, Acknowledged, Dispatched, Cleared }

public class AlarmEvent
{
    public int Id { get; set; }
    public int? SiteId { get; set; }
    public Site? Site { get; set; }
    public required string AccountNumber { get; set; }
    public DateTime ReceivedAtUtc { get; set; }
    public required string EventCode { get; set; }
    public required string Description { get; set; }
    public required string Qualifier { get; set; }
    public string Zone { get; set; } = "000";
    public string Group { get; set; } = "00";
    public AlarmPriority Priority { get; set; }
    public AlarmStatus Status { get; set; } = AlarmStatus.New;
    public string? RawMessage { get; set; }

    public Job? Job { get; set; }
}

public enum JobStatus { Dispatched, EnRoute, OnSite, Completed }

public class Job
{
    public int Id { get; set; }
    public int AlarmEventId { get; set; }
    public AlarmEvent? AlarmEvent { get; set; }
    public required string OfficerName { get; set; }
    public JobStatus Status { get; set; } = JobStatus.Dispatched;
    public DateTime DispatchedAtUtc { get; set; }
    public DateTime? EnRouteAtUtc { get; set; }
    public DateTime? OnSiteAtUtc { get; set; }
    public DateTime? CompletedAtUtc { get; set; }
    public string? Notes { get; set; }
}

public class AuditEntry
{
    public int Id { get; set; }
    public DateTime AtUtc { get; set; }
    public required string Actor { get; set; }
    public required string Action { get; set; }
    public required string Details { get; set; }
}
