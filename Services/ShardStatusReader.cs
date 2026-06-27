using System.Text.Json;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal static class ShardStatusReader
{
    private static readonly TimeSpan MaxFileAge = TimeSpan.FromSeconds(30);

    public static string StatusFilePath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            LauncherConstants.AppDataFolderName,
            "launcher-status.json");

    public static ServerStatusSnapshot? TryReadSnapshot(DateTime easternTime)
    {
        try
        {
            var path = StatusFilePath;
            if (!File.Exists(path))
            {
                return null;
            }

            var age = DateTime.UtcNow - File.GetLastWriteTimeUtc(path);
            if (age > MaxFileAge)
            {
                return null;
            }

            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            return ParseSnapshot(doc.RootElement, easternTime);
        }
        catch
        {
            return null;
        }
    }

    public static ServerStatusSnapshot? ParseSnapshot(JsonElement root, DateTime easternTime)
    {
        var status = ParseStatus(root);
        int? players = root.TryGetProperty("playersOnline", out var p) && p.TryGetInt32(out var pc) ? pc : null;
        TimeSpan? uptime = null;
        if (root.TryGetProperty("uptimeSeconds", out var u) && u.TryGetInt64(out var sec))
        {
            uptime = TimeSpan.FromSeconds(sec);
        }

        string? version = null;
        if (root.TryGetProperty("version", out var versionProp))
        {
            version = versionProp.GetString();
        }

        return new ServerStatusSnapshot
        {
            Status = status,
            PlayersOnline = players,
            ServerUptime = uptime,
            ServerVersion = version,
            EasternTime = easternTime
        };
    }

    private static ShardConnectionStatus ParseStatus(JsonElement root)
    {
        if (!root.TryGetProperty("status", out var statusProp))
        {
            return ShardConnectionStatus.Disconnected;
        }

        return statusProp.GetString()?.ToLowerInvariant() switch
        {
            "online" => ShardConnectionStatus.Online,
            "pending_restart" or "pendingrestart" or "restarting" => ShardConnectionStatus.PendingRestart,
            _ => ShardConnectionStatus.Disconnected
        };
    }
}
