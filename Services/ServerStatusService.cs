using System.Net.Http;
using System.Text.Json;
using System.Windows.Threading;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal sealed class ServerStatusService : IDisposable
{
    private static readonly TimeZoneInfo EasternTimeZone =
        TimeZoneInfo.FindSystemTimeZoneById("Eastern Standard Time");

    private readonly HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(4) };
    private readonly DispatcherTimer? _timer;

    public event Action<ServerStatusSnapshot>? StatusUpdated;

    public ServerStatusSnapshot Latest { get; private set; } = new();

    public ServerStatusService()
    {
        if (System.Windows.Application.Current?.Dispatcher != null)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(15) };
            _timer.Tick += async (_, _) => await RefreshAsync();
            _timer.Start();
        }
    }

    public async Task RefreshAsync()
    {
        StatusUpdated?.Invoke(new ServerStatusSnapshot
        {
            Status = ShardConnectionStatus.Checking,
            EasternTime = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EasternTimeZone)
        });

        var snapshot = await QueryAsync();
        Latest = snapshot;
        StatusUpdated?.Invoke(snapshot);
    }

    private async Task<ServerStatusSnapshot> QueryAsync()
    {
        var eastern = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, EasternTimeZone);

        if (!string.IsNullOrWhiteSpace(LauncherConstants.StatusApiUrl))
        {
            try
            {
                var json = await _http.GetStringAsync(LauncherConstants.StatusApiUrl);
                using var doc = JsonDocument.Parse(json);
                var snapshot = ShardStatusReader.ParseSnapshot(doc.RootElement, eastern);
                if (snapshot != null)
                {
                    return snapshot;
                }
            }
            catch
            {
                // Remote status unavailable.
            }
        }

        var fromFile = ShardStatusReader.TryReadSnapshot(eastern);
        if (fromFile != null)
        {
            return fromFile;
        }

        return new ServerStatusSnapshot
        {
            Status = ShardConnectionStatus.Disconnected,
            PlayersOnline = null,
            ServerUptime = null,
            EasternTime = eastern
        };
    }

    public void Dispose() => _http.Dispose();
}
