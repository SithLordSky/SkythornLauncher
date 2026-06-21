using System.Runtime.InteropServices;
using System.Text;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;

namespace SkythornLauncher.Services;

internal interface ILauncherMusicPlayback : IDisposable
{
    bool IsPlaying { get; }
    void Play(string path, float volume);
    void Stop();
}

internal sealed class MidiLauncherMusicPlayback : ILauncherMusicPlayback
{
    private const string Alias = "SkythornLauncherMusic";

    private DispatcherTimer? _loopTimer;
    private bool _deviceOpen;

    public bool IsPlaying { get; private set; }

    public void Play(string path, float volume)
    {
        Stop();

        if (!Send($"open \"{path.Replace("\"", "'")}\" type sequencer alias {Alias}"))
        {
            return;
        }

        _deviceOpen = true;
        SetVolume(volume);

        if (!Send($"play {Alias}"))
        {
            Send($"close {Alias}");
            _deviceOpen = false;
            return;
        }

        IsPlaying = true;
        StartLoopMonitor();
    }

    public void Stop()
    {
        StopLoopMonitor();

        if (_deviceOpen)
        {
            Send($"stop {Alias}");
            Send($"close {Alias}");
            _deviceOpen = false;
        }

        IsPlaying = false;
    }

    public void Dispose() => Stop();

    private void SetVolume(float volume)
    {
        if (!_deviceOpen)
        {
            return;
        }

        var mciVolume = (int)Math.Clamp(volume * 1000f, 0f, 1000f);
        Send($"setaudio {Alias} volume to {mciVolume}");
    }

    private void StartLoopMonitor()
    {
        StopLoopMonitor();

        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher == null)
        {
            return;
        }

        _loopTimer = new DispatcherTimer(DispatcherPriority.Background, dispatcher)
        {
            Interval = TimeSpan.FromMilliseconds(750)
        };
        _loopTimer.Tick += (_, _) =>
        {
            if (!IsPlaying || !_deviceOpen)
            {
                return;
            }

            var mode = QueryStatus("mode");
            if (string.Equals(mode, "stopped", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(mode, "not ready", StringComparison.OrdinalIgnoreCase))
            {
                Send($"play {Alias}");
            }
        };
        _loopTimer.Start();
    }

    private void StopLoopMonitor()
    {
        if (_loopTimer == null)
        {
            return;
        }

        _loopTimer.Stop();
        _loopTimer = null;
    }

    private static string QueryStatus(string item)
    {
        var buffer = new StringBuilder(128);
        return Send($"status {Alias} {item}", buffer) ? buffer.ToString().Trim() : string.Empty;
    }

    private static bool Send(string command) => Send(command, null);

    private static bool Send(string command, StringBuilder? response)
    {
        if (mciSendString(command, response, response?.Capacity ?? 0, IntPtr.Zero) == 0)
        {
            return true;
        }

        return false;
    }

    [DllImport("winmm.dll", CharSet = CharSet.Unicode)]
    private static extern int mciSendString(string command, StringBuilder? buffer, int bufferSize, IntPtr hwndCallback);
}

internal sealed class Mp3LauncherMusicPlayback : ILauncherMusicPlayback
{
    private MediaPlayer? _player;
    private float _volume = 0.4f;

    public bool IsPlaying => _player != null;

    public void Play(string path, float volume)
    {
        Stop();
        _volume = volume;
        _player = new MediaPlayer();
        _player.MediaEnded += OnMediaEnded;
        _player.Open(new Uri(path, UriKind.Absolute));
        _player.Volume = volume;
        _player.Play();
    }

    public void Stop()
    {
        if (_player == null)
        {
            return;
        }

        _player.MediaEnded -= OnMediaEnded;
        _player.Stop();
        _player.Close();
        _player = null;
    }

    public void Dispose() => Stop();

    private void OnMediaEnded(object? sender, EventArgs e)
    {
        if (_player == null)
        {
            return;
        }

        _player.Position = TimeSpan.Zero;
        _player.Volume = _volume;
        _player.Play();
    }
}

internal static class LauncherMusicPlaybackFactory
{
    public static ILauncherMusicPlayback Create(string path)
    {
        return Path.GetExtension(path).Equals(".mp3", StringComparison.OrdinalIgnoreCase)
            ? new Mp3LauncherMusicPlayback()
            : new MidiLauncherMusicPlayback();
    }
}
