namespace SkythornLauncher.Models;

public sealed class LauncherProfile
{
    public string Name { get; set; } = "Default";
    public string Username { get; set; } = string.Empty;
    public string PasswordProtected { get; set; } = string.Empty;
    public string UltimaOnlineDirectory { get; set; } = LauncherConstants.DefaultUltimaOnlinePath;
    public bool SaveAccount { get; set; }
    public DateTime LastUsedUtc { get; set; }
    public ClientPreferences Preferences { get; set; } = new();

    public bool HasSavedPassword => SaveAccount && !string.IsNullOrEmpty(PasswordProtected);
}
