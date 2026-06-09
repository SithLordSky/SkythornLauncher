namespace SkythornLauncher;

internal static class LauncherConstants
{
    public const string ShardName = "Lord Skythorn's Revenge";
    // Local testing — change back to 192.168.0.1 for production v1.0.0
    public const string ServerIp = "127.0.0.1";
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
    /// HTTP status endpoint (ShardStatusServer). Launchers never probe game port 2593.
    /// </summary>
    public const string StatusApiUrl = "http://127.0.0.1:8080/status.json";

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
