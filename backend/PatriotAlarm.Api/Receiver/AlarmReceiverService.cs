using System.Net;
using System.Net.Sockets;
using System.Text;
using PatriotAlarm.Api.Services;

namespace PatriotAlarm.Api.Receiver;

/// <summary>
/// Listens on a TCP port for alarm panels or signalling converters that send Contact ID lines.
/// Each valid message is stored and acknowledged with ACK (0x06); bad messages get NAK (0x15).
/// </summary>
public class AlarmReceiverService(
    IServiceScopeFactory scopes,
    IConfiguration configuration,
    ILogger<AlarmReceiverService> logger) : BackgroundService
{
    private const byte Ack = 0x06;
    private const byte Nak = 0x15;

    // Common frame terminators: CR, LF, and ETX (0x14) used by many receivers
    private static readonly char[] Terminators = ['\r', '\n', '\u0014'];

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var port = configuration.GetValue("Receiver:Port", 5050);
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();
        logger.LogInformation("Alarm receiver listening on port {Port}", port);

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var client = await listener.AcceptTcpClientAsync(stoppingToken);
                _ = HandleClientAsync(client, stoppingToken);
            }
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown
        }
        finally
        {
            listener.Stop();
        }
    }

    private async Task HandleClientAsync(TcpClient client, CancellationToken ct)
    {
        using (client)
        {
            var remote = client.Client.RemoteEndPoint?.ToString() ?? "unknown";
            logger.LogInformation("Receiver connection from {Remote}", remote);

            var stream = client.GetStream();
            var buffer = new byte[1024];
            var pending = new StringBuilder();

            while (!ct.IsCancellationRequested)
            {
                int read;
                try
                {
                    read = await stream.ReadAsync(buffer, ct);
                }
                catch (IOException)
                {
                    break;
                }
                if (read == 0)
                {
                    break;
                }

                pending.Append(Encoding.ASCII.GetString(buffer, 0, read));
                await ProcessFramesAsync(pending, stream, ct);
            }

            logger.LogInformation("Receiver connection closed from {Remote}", remote);
        }
    }

    private async Task ProcessFramesAsync(StringBuilder pending, NetworkStream stream, CancellationToken ct)
    {
        var text = pending.ToString();
        var lastTerminator = text.LastIndexOfAny(Terminators);
        if (lastTerminator < 0)
        {
            return;
        }

        var complete = text[..(lastTerminator + 1)];
        pending.Remove(0, lastTerminator + 1);

        foreach (var frame in complete.Split(Terminators, StringSplitOptions.RemoveEmptyEntries))
        {
            var parsed = ContactIdParser.Parse(frame);
            if (parsed is null)
            {
                logger.LogWarning("Rejected malformed message: {Frame}", frame);
                await stream.WriteAsync(new[] { Nak }, ct);
                continue;
            }

            using var scope = scopes.CreateScope();
            var alarms = scope.ServiceProvider.GetRequiredService<AlarmService>();
            await alarms.IngestAsync(parsed, frame, ct);

            logger.LogInformation("Stored {Description} for account {Account}", parsed.Description, parsed.AccountNumber);
            await stream.WriteAsync(new[] { Ack }, ct);
        }
    }
}
