using System.Net;
using System.Text;
using System.Text.Json;

namespace ShardStatusServer;

internal static class Program
{
    private const string ListenPrefix = "http://127.0.0.1:8080/";
    private static readonly TimeSpan MaxStatusFileAge = TimeSpan.FromSeconds(30);

    private static readonly string StatusFilePath =
        Environment.GetEnvironmentVariable("LSR_STATUS_FILE")
        ?? Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "LordSkythornsRevenge",
            "launcher-status.json");

    private static async Task Main()
    {
        Console.WriteLine("Lord Skythorn's Revenge — Shard Status Server");
        Console.WriteLine($"HTTP: {ListenPrefix}status.json");
        Console.WriteLine($"Status file: {StatusFilePath}");
        Console.WriteLine("Reads RunUO launcher-status.json — no game-port probes, no log spam.");
        Console.WriteLine("Press Ctrl+C to stop.");
        Console.WriteLine();

        using var cts = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cts.Cancel();
        };

        using var listener = new HttpListener();
        listener.Prefixes.Add(ListenPrefix);
        listener.Start();

        try
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var context = await listener.GetContextAsync().WaitAsync(cts.Token);
                _ = Task.Run(() => HandleRequestAsync(context), cts.Token);
            }
        }
        catch (OperationCanceledException)
        {
            // shutting down
        }
        finally
        {
            listener.Stop();
        }
    }

    private static async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            if (!context.Request.Url?.AbsolutePath.Equals("/status.json", StringComparison.OrdinalIgnoreCase) ?? true)
            {
                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.Close();
                return;
            }

            var snapshot = ReadStatusFile();
            var json = JsonSerializer.Serialize(new
            {
                status = snapshot.Status,
                playersOnline = snapshot.PlayersOnline,
                uptimeSeconds = snapshot.UptimeSeconds
            });

            var buffer = Encoding.UTF8.GetBytes(json);
            context.Response.StatusCode = (int)HttpStatusCode.OK;
            context.Response.ContentType = "application/json";
            context.Response.ContentEncoding = Encoding.UTF8;
            context.Response.ContentLength64 = buffer.Length;
            context.Response.Headers.Add("Cache-Control", "no-cache");
            await context.Response.OutputStream.WriteAsync(buffer);
            context.Response.Close();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Request error: {ex.Message}");
            try
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.Close();
            }
            catch
            {
                // ignore
            }
        }
    }

    private static StatusSnapshot ReadStatusFile()
    {
        try
        {
            if (!File.Exists(StatusFilePath))
            {
                return Disconnected();
            }

            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(StatusFilePath);
            if (age > MaxStatusFileAge)
            {
                return Disconnected();
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(StatusFilePath));
            var root = doc.RootElement;

            var status = root.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString() ?? "disconnected"
                : "disconnected";

            int? players = root.TryGetProperty("playersOnline", out var playersProp) &&
                            playersProp.TryGetInt32(out var playerCount)
                ? playerCount
                : null;

            long? uptime = root.TryGetProperty("uptimeSeconds", out var uptimeProp) &&
                           uptimeProp.TryGetInt64(out var uptimeSeconds)
                ? uptimeSeconds
                : null;

            return new StatusSnapshot(status, players, uptime);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Status file error: {ex.Message}");
            return Disconnected();
        }
    }

    private static StatusSnapshot Disconnected() => new("disconnected", null, null);
}

internal readonly record struct StatusSnapshot(string Status, int? PlayersOnline, long? UptimeSeconds);
