namespace SkythornLauncher.Models;

public sealed class ClientPreferences
{
    public bool AutoLoginOnPlay { get; set; }
    public bool SkipLoginScreen { get; set; }
    public bool AutoLogin { get; set; }
    public bool AutoReconnect { get; set; }
    public int ReconnectDelayMs { get; set; } = 1000;
    public bool SaveAccount { get; set; }
    public bool EnablePacketLog { get; set; }
    public bool EnableMusic { get; set; } = true;
    public bool HighDpi { get; set; }
    public int MusicVolume { get; set; } = 50;
    public int FootstepsVolume { get; set; } = 75;
    public byte ForceDriver { get; set; }
}
