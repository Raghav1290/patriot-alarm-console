using Microsoft.AspNetCore.SignalR;

namespace PatriotAlarm.Api.Hubs;

/// <summary>
/// Pushes live alarm and job changes to operator consoles.
/// Clients only listen; all changes go through the REST controllers.
/// </summary>
public class AlarmHub : Hub
{
    public const string EventChanged = "EventChanged";
    public const string JobChanged = "JobChanged";
}
