using PatriotAlarm.Api.Domain;
using PatriotAlarm.Api.Receiver;
using Xunit;

namespace PatriotAlarm.Tests;

public class ContactIdParserTests
{
    [Fact]
    public void Parses_a_burglary_alarm()
    {
        var parsed = ContactIdParser.Parse("1001181130010010");

        Assert.NotNull(parsed);
        Assert.Equal("1001", parsed.AccountNumber);
        Assert.Equal("1", parsed.Qualifier);
        Assert.Equal("130", parsed.EventCode);
        Assert.Equal("Burglary alarm", parsed.Description);
        Assert.Equal("01", parsed.Group);
        Assert.Equal("001", parsed.Zone);
        Assert.Equal(AlarmPriority.High, parsed.Priority);
    }

    [Fact]
    public void Fire_alarm_is_critical()
    {
        var parsed = ContactIdParser.Parse("1002181110000010");

        Assert.NotNull(parsed);
        Assert.Equal(AlarmPriority.Critical, parsed.Priority);
    }

    [Fact]
    public void Unknown_event_codes_are_kept_with_medium_priority()
    {
        var parsed = ContactIdParser.Parse("1003181999010010");

        Assert.NotNull(parsed);
        Assert.Equal("Event 999", parsed.Description);
        Assert.Equal(AlarmPriority.Medium, parsed.Priority);
    }

    [Fact]
    public void Surrounding_whitespace_is_ignored()
    {
        var parsed = ContactIdParser.Parse("  1001181130010010\r\n");

        Assert.NotNull(parsed);
        Assert.Equal("1001", parsed.AccountNumber);
    }

    [Theory]
    [InlineData("")]
    [InlineData("hello")]
    [InlineData("100118113001001")]      // one digit short
    [InlineData("10011811300100100")]    // one digit too long
    [InlineData("1001191130010010")]     // wrong message type (not 18)
    [InlineData("1001189130010010")]     // qualifier 9 is not 1, 3 or 6
    public void Rejects_malformed_messages(string message)
    {
        Assert.Null(ContactIdParser.Parse(message));
    }
}
