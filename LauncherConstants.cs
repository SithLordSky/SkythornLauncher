namespace SkythornLauncher;

internal static class LauncherConstants
{
    public const string ShardName = "Lord Skythorn's Revenge";
    public const string ServerIp = "195.88.56.169";
    public const ushort ServerPort = 2593;
    public const string ClientVersion = "3.0.8q";
    public const string DefaultUltimaOnlinePath = @"C:\ProgramFiles\Ultima Online 2D";

    public const string AppDataFolderName = "LordSkythornsRevenge";
    public const string ProfilesFileName = "profiles.json";

    /// <summary>Folder name under <see cref="InstallRoot"/> for ClassicUO settings, Data, and logs.</summary>
    public const string ClientDataFolderName = "ClassicUO";

    /// <summary>Folder name under <see cref="InstallRoot"/> for cuo.dll and runtime dependencies.</summary>
    public const string ClientBinaryFolderName = "Client";

    /// <summary>
    /// Remote shard status HTTP endpoint (ShardStatusServer on the RunUO machine).
    /// Launchers never probe game port 2593 and never bundle the sidecar exe.
    /// </summary>
    public const string StatusApiUrl = "http://195.88.56.169:8080/status.json";

    /// <summary>
    /// Wix blog RSS feed used to populate the launcher News panel.
    /// </summary>
    public const string NewsFeedUrl = "https://xsithlordskyx.wixsite.com/lord-skythorn/blog-feed.xml";

    /// <summary>Public website opened when news cannot be loaded or the user clicks fallback text.</summary>
    public const string WebsiteUrl = "https://xsithlordskyx.wixsite.com/lord-skythorn";

    public const string GitHubOwner = "SithLordSky";
    public const string GitHubRepo = "SkythornLauncher";

    /// <summary>Latest GitHub Release (test builds are cut from private-test-fixes only).</summary>
    public const string GitHubLatestReleaseApiUrl =
        "https://api.github.com/repos/SithLordSky/SkythornLauncher/releases/latest";

    public static string LauncherVersion => LauncherVersionInfo.Display;

    /// <summary>
    /// Root of the portable install. Walks up from the running exe until it finds
    /// <c>ClassicUO/</c> (data) and <c>Client/cuo.dll</c> (binaries).
    /// </summary>
    public static string InstallRoot => ResolveInstallRoot();

    public static string ClientDataDirectory =>
        Path.Combine(InstallRoot, ClientDataFolderName);

    public static string ClientBinaryDirectory =>
        Path.Combine(InstallRoot, ClientBinaryFolderName);

    public static string ClientDllPath =>
        Path.Combine(ClientBinaryDirectory, "cuo.dll");

    public static string SettingsJsonPath =>
        Path.Combine(ClientDataDirectory, "settings.json");

    public static string ProfilesPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            AppDataFolderName,
            ProfilesFileName);

    public static string LaunchLogPath =>
        Path.Combine(ClientDataDirectory, "Logs", "launcher-client.log");

    public static string UpdateStagingRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolderName,
            "update-staging");

    public static string UpdateBackupRoot =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            AppDataFolderName,
            "update-backup");

    public static string UpdaterExePath =>
        Path.Combine(InstallRoot, "SkythornUpdater.exe");

    public static string LauncherExePath =>
        Path.Combine(InstallRoot, "SkythornLauncher.exe");

    private static string ResolveInstallRoot()
    {
        var dir = AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        for (var i = 0; i < 6; i++)
        {
            if (Directory.Exists(Path.Combine(dir, ClientDataFolderName)) &&
                File.Exists(Path.Combine(dir, ClientBinaryFolderName, "cuo.dll")))
            {
                return dir;
            }

            dir = Path.GetFullPath(Path.Combine(dir, ".."));
        }

        return AppContext.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }
}
