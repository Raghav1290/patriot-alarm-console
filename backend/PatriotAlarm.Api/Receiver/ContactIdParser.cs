using System.Text.RegularExpressions;
using PatriotAlarm.Api.Domain;

namespace PatriotAlarm.Api.Receiver;

public record ParsedAlarm(
    string AccountNumber,
    string Qualifier,
    string EventCode,
    string Description,
    string Group,
    string Zone,
    AlarmPriority Priority);

/// <summary>
/// Parses a 16-digit Contact ID message:
/// AAAA (account) 18 (message type) Q (qualifier) XYZ (event code) GG (group) ZZZ (zone) C (checksum)
/// Checksum is read but not validated yet.
/// </summary>
public static partial class ContactIdParser
{
    [GeneratedRegex(@"^(\d{4})18([136])(\d{3})(\d{2})(\d{3})(\d)$")]
    private static partial Regex MessagePattern();

    private static readonly Dictionary<string, (string Description, AlarmPriority Priority)> EventCodes = new()
    {
        ["110"] = ("Fire alarm", AlarmPriority.Critical),
        ["120"] = ("Panic alarm", AlarmPriority.Critical),
        ["130"] = ("Burglary alarm", AlarmPriority.High),
        ["131"] = ("Perimeter alarm", AlarmPriority.High),
        ["137"] = ("Tamper", AlarmPriority.Medium),
        ["140"] = ("General alarm", AlarmPriority.High),
        ["300"] = ("System trouble", AlarmPriority.Medium),
        ["302"] = ("Low battery", AlarmPriority.Medium),
        ["401"] = ("Open / close", AlarmPriority.Low),
        ["570"] = ("Zone bypass", AlarmPriority.Low),
        ["602"] = ("Periodic test", AlarmPriority.Info),
    };

    public static ParsedAlarm? Parse(string message)
    {
        var match = MessagePattern().Match(message.Trim());
        if (!match.Success)
        {
            return null;
        }

        var eventCode = match.Groups[3].Value;
        var (description, priority) = EventCodes.TryGetValue(eventCode, out var known)
            ? known
            : ($"Event {eventCode}", AlarmPriority.Medium);

        return new ParsedAlarm(
            AccountNumber: match.Groups[1].Value,
            Qualifier: match.Groups[2].Value,
            EventCode: eventCode,
            Description: description,
            Group: match.Groups[4].Value,
            Zone: match.Groups[5].Value,
            Priority: priority);
    }
}
