using System.IO;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SkythornLauncher;

internal static class UiFormat
{
    public static string TruncatePath(string path, int maxLength = 34)
    {
        if (string.IsNullOrWhiteSpace(path) || path.Length <= maxLength)
        {
            return string.IsNullOrWhiteSpace(path) ? "Not set" : path;
        }

        return "..." + path[^Math.Min(maxLength - 3, path.Length)..];
    }

    public static string TrimExcerpt(string? text, int maxLength = 110)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var normalized = text.Trim();
        if (normalized.Length <= maxLength)
        {
            return normalized;
        }

        return normalized[..maxLength].TrimEnd() + "...";
    }

    public static string FormatNewsDate(DateTime? date) =>
        date?.ToLocalTime().ToString("MMM d, yyyy") ?? string.Empty;

    public static string FormatUptime(TimeSpan? uptime)
    {
        if (uptime == null)
        {
            return "—";
        }

        var value = uptime.Value;
        if (value.TotalDays >= 1)
        {
            return $"{(int)value.TotalDays}d {value.Hours}h {value.Minutes}m";
        }

        if (value.TotalHours >= 1)
        {
            return $"{(int)value.TotalHours}h {value.Minutes}m";
        }

        return $"{value.Minutes}m {value.Seconds}s";
    }

    public static Brush StatusBrush(Models.ShardConnectionStatus status) => status switch
    {
        Models.ShardConnectionStatus.Online => new SolidColorBrush(Color.FromRgb(40, 200, 80)),
        Models.ShardConnectionStatus.Checking or Models.ShardConnectionStatus.PendingRestart =>
            new SolidColorBrush(Color.FromRgb(230, 190, 40)),
        _ => new SolidColorBrush(Color.FromRgb(210, 50, 50))
    };

    public static string StatusText(Models.ShardConnectionStatus status) => status switch
    {
        Models.ShardConnectionStatus.Online => "Online",
        Models.ShardConnectionStatus.Checking => "Checking...",
        Models.ShardConnectionStatus.PendingRestart => "Pending Restart",
        _ => "Disconnected"
    };

    public static ImageSource StatusIcon(Models.ShardConnectionStatus status)
    {
        var fileName = status switch
        {
            Models.ShardConnectionStatus.Online => "status-green.png",
            Models.ShardConnectionStatus.Checking or Models.ShardConnectionStatus.PendingRestart =>
                "status-yellow.png",
            _ => "status-red.png"
        };

        var path = Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", fileName);
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        return image;
    }
}
