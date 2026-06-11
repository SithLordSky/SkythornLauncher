using System.Windows.Media;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal sealed class LauncherMusicService : IDisposable
{
    private const string TrackFileName = "Britainpos.mp3";
    private const double MenuVolume = 0.6;

    private MediaPlayer? _player;
    private bool _disposed;

    public void Play(LauncherProfile profile)
    {
        Stop();

        if (!SettingsWriter.ReadPreferences().EnableMusic)
        {
            return;
        }

        var trackPath = ResolveTrackPath(LauncherMusicService.ResolveUltimaOnlineDirectory(profile));
        if (trackPath == null)
        {
            return;
        }

        _player = new MediaPlayer();
        _player.MediaOpened += (_, _) =>
        {
            if (_player == null)
            {
                return;
            }

            _player.Volume = MenuVolume;
            _player.Play();
        };
        _player.MediaFailed += (_, args) =>
        {
            System.Diagnostics.Debug.WriteLine($"Launcher music failed: {args.ErrorException?.Message}");
            Stop();
        };
        _player.MediaEnded += (_, _) =>
        {
            if (_player == null)
            {
                return;
            }

            _player.Position = TimeSpan.Zero;
            _player.Volume = MenuVolume;
            _player.Play();
        };

        _player.Open(new Uri(trackPath, UriKind.Absolute));
    }

    public void Stop()
    {
        if (_player == null)
        {
            return;
        }

        _player.Close();
        _player = null;
    }

    public static string? ResolveTrackPath(string ultimaOnlineDirectory)
    {
        if (string.IsNullOrWhiteSpace(ultimaOnlineDirectory))
        {
            return null;
        }

        var digitalDir = Path.Combine(ultimaOnlineDirectory, "Music", "Digital");
        if (!Directory.Exists(digitalDir))
        {
            return null;
        }

        var exactPath = Path.Combine(digitalDir, TrackFileName);
        if (File.Exists(exactPath))
        {
            return exactPath;
        }

        return Directory
            .GetFiles(digitalDir, "*.mp3", SearchOption.TopDirectoryOnly)
            .FirstOrDefault(path => string.Equals(Path.GetFileName(path), TrackFileName, StringComparison.OrdinalIgnoreCase));
    }

    public static string ResolveUltimaOnlineDirectory(LauncherProfile profile)
    {
        if (!string.IsNullOrWhiteSpace(profile.UltimaOnlineDirectory) &&
            Directory.Exists(profile.UltimaOnlineDirectory))
        {
            return profile.UltimaOnlineDirectory;
        }

        var settingsPath = LauncherConstants.SettingsJsonPath;
        if (File.Exists(settingsPath))
        {
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(settingsPath));
                if (doc.RootElement.TryGetProperty("ultimaonlinedirectory", out var element))
                {
                    var path = element.GetString();
                    if (!string.IsNullOrWhiteSpace(path) && Directory.Exists(path))
                    {
                        return path;
                    }
                }
            }
            catch
            {
                // fall through to default
            }
        }

        return LauncherConstants.DefaultUltimaOnlinePath;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Stop();
        _disposed = true;
    }
}
