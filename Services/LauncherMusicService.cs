using System.Windows;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

/// <summary>
/// Plays Create1 from the profile UO install while the launcher is idle.
/// Stops when Play launches the game; resumes when the game exits.
/// </summary>
internal sealed class LauncherMusicService : IDisposable
{
    public static LauncherMusicService Instance { get; } = new();

    private readonly object _gate = new();
    private ILauncherMusicPlayback? _playback;
    private string? _musicPath;
    private string? _ultimaOnlineDirectory;
    private bool _pausedForGame;

    private LauncherMusicService()
    {
    }

    public void SyncWithProfile(LauncherProfile profile)
    {
        RunOnUiThread(() =>
        {
            lock (_gate)
            {
                _ultimaOnlineDirectory = profile.UltimaOnlineDirectory;
                _musicPath = ResolveMusicPath(profile.UltimaOnlineDirectory);

                if (_pausedForGame || GameProcessTracker.IsGameRunning())
                {
                    return;
                }

                if (_musicPath == null)
                {
                    StopPlaybackInternal();
                    return;
                }

                if (_playback == null || !_playback.IsPlaying)
                {
                    StartPlaybackInternal();
                }
            }
        });
    }

    public void StopForGameLaunch()
    {
        RunOnUiThread(() =>
        {
            lock (_gate)
            {
                _pausedForGame = true;
                StopPlaybackInternal();
            }
        });
    }

    public void ResumeAfterGame()
    {
        RunOnUiThread(() =>
        {
            lock (_gate)
            {
                _pausedForGame = false;

                if (GameProcessTracker.IsGameRunning())
                {
                    return;
                }

                if (_musicPath == null || !File.Exists(_musicPath))
                {
                    return;
                }

                StartPlaybackInternal();
            }
        });
    }

    public void Dispose()
    {
        RunOnUiThread(() =>
        {
            lock (_gate)
            {
                StopPlaybackInternal();
                _pausedForGame = false;
            }
        });
    }

    private void StartPlaybackInternal()
    {
        if (string.IsNullOrEmpty(_musicPath) || !File.Exists(_musicPath))
        {
            return;
        }

        try
        {
            _playback?.Dispose();
            _playback = LauncherMusicPlaybackFactory.Create(_musicPath);
            _playback.Play(_musicPath, LauncherConstants.LauncherMusicVolume);
        }
        catch
        {
            _playback?.Dispose();
            _playback = null;
        }
    }

    private void StopPlaybackInternal()
    {
        _playback?.Stop();
        _playback?.Dispose();
        _playback = null;
    }

    private static string? ResolveMusicPath(string ultimaOnlineDirectory)
    {
        if (string.IsNullOrWhiteSpace(ultimaOnlineDirectory))
        {
            return null;
        }

        var folder = Path.Combine(ultimaOnlineDirectory, LauncherConstants.LauncherMusicSubfolder);
        if (!Directory.Exists(folder))
        {
            return null;
        }

        foreach (var extension in new[] { ".mid", ".mp3" })
        {
            var direct = Path.Combine(folder, LauncherConstants.LauncherMusicBaseName + extension);
            if (File.Exists(direct))
            {
                return direct;
            }
        }

        try
        {
            return Directory
                .EnumerateFiles(folder, "*.*", SearchOption.TopDirectoryOnly)
                .FirstOrDefault(path =>
                    string.Equals(
                        Path.GetFileNameWithoutExtension(path),
                        LauncherConstants.LauncherMusicBaseName,
                        StringComparison.OrdinalIgnoreCase));
        }
        catch
        {
            return null;
        }
    }

    private static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }
}
