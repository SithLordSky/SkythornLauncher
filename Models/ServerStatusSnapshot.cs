namespace SkythornLauncher.Models;

public enum ShardConnectionStatus
{
    Checking,
    Online,
    Disconnected,
    PendingRestart
}

public sealed class ServerStatusSnapshot
{
    public ShardConnectionStatus Status { get; init; } = ShardConnectionStatus.Disconnected;
    public int? PlayersOnline { get; init; }
    public TimeSpan? ServerUptime { get; init; }
    public string? ServerVersion { get; init; }
    public DateTime EasternTime { get; init; } = DateTime.Now;
}
