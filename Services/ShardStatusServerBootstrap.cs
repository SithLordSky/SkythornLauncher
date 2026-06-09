using System.Diagnostics;
using System.Net.Http;

namespace SkythornLauncher.Services;

internal static class ShardStatusServerBootstrap
{
    private static bool _startAttempted;

    public static async Task EnsureRunningAsync(HttpClient http)
    {
        if (_startAttempted || string.IsNullOrWhiteSpace(LauncherConstants.StatusApiUrl))
        {
            return;
        }

        if (await IsApiRespondingAsync(http))
        {
            return;
        }

        _startAttempted = true;

        var exePath = Path.Combine(AppContext.BaseDirectory, "StatusServer", "ShardStatusServer.exe");
        if (!File.Exists(exePath))
        {
            return;
        }

        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = exePath,
                WorkingDirectory = Path.GetDirectoryName(exePath)!,
                UseShellExecute = false,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden
            });

            await Task.Delay(1500);
        }
        catch
        {
            // Fall back to direct status file read.
        }
    }

    private static async Task<bool> IsApiRespondingAsync(HttpClient http)
    {
        try
        {
            using var response = await http.GetAsync(LauncherConstants.StatusApiUrl);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
