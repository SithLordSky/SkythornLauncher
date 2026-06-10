namespace SkythornLauncher;

/// <summary>
/// Launcher display version. Bump <see cref="Patch"/> by 1 on every published update (+0.0.1).
/// When Patch is 9, the next bump rolls over: 0.1.9 → 0.2.0 → 0.2.1 … → 0.2.9 → 0.3.0, and so on.
/// </summary>
internal static class LauncherVersionInfo
{
    public const int Major = 0;
    public const int Minor = 2;
    public const int Patch = 1;

    public static string Display => $"{Major}.{Minor}.{Patch}";
}
