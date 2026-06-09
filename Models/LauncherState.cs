namespace SkythornLauncher.Models;

public sealed class LauncherState
{
    public string ActiveProfileName { get; set; } = "Default";
    public List<LauncherProfile> Profiles { get; set; } = [];
}
