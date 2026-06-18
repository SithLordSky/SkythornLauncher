namespace SkythornLauncher;

/// <summary>
/// Launcher display version. Playtest line: 0.9.x; bump <see cref="Patch"/> for playtest fixes.
/// After playtest: 1.0.0. During dev, bump Patch by 1 on each publish (+0.0.1).
/// </summary>
internal static class LauncherVersionInfo
{
    public const int Major = 0;
    public const int Minor = 9;
    public const int Patch = 6;

    public static string Display => $"{Major}.{Minor}.{Patch}";
}
