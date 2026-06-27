using System.Diagnostics;

using System.Text;

using SkythornLauncher.Models;



namespace SkythornLauncher.Services;



internal sealed class GameLaunchResult

{

    public required Process Process { get; init; }

    public string? EarlyError { get; init; }

}



internal static class GameLauncher

{

    public static void EnsureClientBuilt()

    {

        if (File.Exists(LauncherConstants.ClientDllPath))

        {

            return;

        }



        throw new FileNotFoundException(

            "The ClassicUO client is missing from this install folder.\n\n" +

            "Expected:\n" +

            "  " + LauncherConstants.ClientDllPath + "\n\n" +

            "Copy or build the client into the Client folder next to this launcher " +

            "(cuo.dll, cuo.exe, and runtime dependencies). Do not rely on ClassicUO-Source " +

            "or other paths outside this folder.\n\n" +

            "Or ask your shard admin for a pre-built Client folder.",

            LauncherConstants.ClientDllPath);

    }



    public static ProcessStartInfo BuildStartInfo(LauncherProfile profile, string password)

    {

        var clientDll = LauncherConstants.ClientDllPath;

        var settingsPath = LauncherConstants.SettingsJsonPath;

        var binaryDirectory = LauncherConstants.ClientBinaryDirectory;

        var dataDirectory = LauncherConstants.ClientDataDirectory;

        var prefs = profile.Preferences;



        EnsureClientBuilt();

        EnsureClientWindowIcon();



        if (!Directory.Exists(dataDirectory))

        {

            throw new DirectoryNotFoundException($"ClassicUO data directory was not found: {dataDirectory}");

        }



        var uoPath = profile.UltimaOnlineDirectory;

        if (string.IsNullOrWhiteSpace(uoPath) || !Directory.Exists(uoPath))

        {

            throw new DirectoryNotFoundException("Ultima Online client files folder was not found. Choose a profile and set the UO folder under Profile or Settings.");

        }



        var tileData = Path.Combine(uoPath, "tiledata.mul");

        if (!File.Exists(tileData))

        {

            throw new FileNotFoundException("The selected folder does not look like a valid Ultima Online client install (tiledata.mul missing).", tileData);

        }



        SettingsWriter.Write(profile, password);



        var clientArgs = new List<string>

        {

            "-settings", Quote(settingsPath),

            "-ip", LauncherConstants.ServerIp,

            "-port", LauncherConstants.ServerPort.ToString(),

            "-clientversion", LauncherConstants.ClientVersion,

            "-ultimaonlinedirectory", Quote(uoPath),

            "-last_server_name", Quote(LauncherConstants.ShardName)

        };



        if (!string.IsNullOrWhiteSpace(profile.Username))

        {

            clientArgs.Add("-username");

            clientArgs.Add(Quote(profile.Username));

        }



        if (!string.IsNullOrWhiteSpace(password))

        {

            clientArgs.Add("-password");

            clientArgs.Add(Quote(password));

        }



        if (profile.SaveAccount || prefs.SaveAccount)

        {

            clientArgs.Add("-saveaccount");

            clientArgs.Add("true");

        }



        if (prefs.AutoLogin || prefs.AutoLoginOnPlay)

        {

            clientArgs.Add("-autologin");

            clientArgs.Add("true");

        }



        if (prefs.AutoReconnect)

        {

            clientArgs.Add("-reconnect");

            clientArgs.Add("true");

            clientArgs.Add("-reconnect_time");

            clientArgs.Add(Math.Max(1000, prefs.ReconnectDelayMs).ToString());

        }



        if (prefs.SkipLoginScreen)

        {

            clientArgs.Add("-skiploginscreen");

        }



        if (prefs.EnablePacketLog)

        {

            clientArgs.Add("-packetlog");

        }



        if (prefs.EnableMusic)

        {

            clientArgs.Add("-login_music");

            clientArgs.Add("true");

            clientArgs.Add("-login_music_volume");

            clientArgs.Add(Math.Clamp(prefs.MusicVolume, 0, 100).ToString());

        }

        else

        {

            clientArgs.Add("-login_music");

            clientArgs.Add("false");

        }



        if (prefs.HighDpi)

        {

            clientArgs.Add("-highdpi");

        }



        if (prefs.ForceDriver > 0)

        {

            clientArgs.Add("-force_driver");

            clientArgs.Add(prefs.ForceDriver.ToString());

        }



        var args = new List<string>

        {

            "exec",

            Quote(clientDll),

            "--"

        };

        args.AddRange(clientArgs);



        var logsDir = Path.GetDirectoryName(LauncherConstants.LaunchLogPath);

        if (!string.IsNullOrEmpty(logsDir) && !Directory.Exists(logsDir))

        {

            Directory.CreateDirectory(logsDir);

        }



        return new ProcessStartInfo

        {

            FileName = "dotnet",

            Arguments = string.Join(' ', args),

            WorkingDirectory = binaryDirectory,

            UseShellExecute = false,

            RedirectStandardError = true,

            RedirectStandardOutput = true,

            CreateNoWindow = true

        };

    }



    public static async Task<GameLaunchResult> LaunchAsync(LauncherProfile profile, string password)

    {

        var startInfo = BuildStartInfo(profile, password);

        var log = new StringBuilder();

        log.AppendLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] Starting client");

        log.AppendLine($"dotnet {startInfo.Arguments}");

        log.AppendLine();



        var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };



        process.OutputDataReceived += (_, e) =>

        {

            if (e.Data != null)

            {

                log.AppendLine(e.Data);

            }

        };



        process.ErrorDataReceived += (_, e) =>

        {

            if (e.Data != null)

            {

                log.AppendLine(e.Data);

            }

        };



        if (!process.Start())

        {

            throw new InvalidOperationException("Failed to start the ClassicUO client process.");

        }



        process.BeginOutputReadLine();

        process.BeginErrorReadLine();



        await Task.Delay(4000);



        string? earlyError = null;



        if (process.HasExited && process.ExitCode != 0)

        {

            earlyError = ReadTail(log.ToString());

            if (string.IsNullOrWhiteSpace(earlyError))

            {

                earlyError = $"ClassicUO exited immediately (code {process.ExitCode}).";

            }

        }



        try

        {

            File.WriteAllText(LauncherConstants.LaunchLogPath, log.ToString());

        }

        catch

        {

            // ignore log write failures

        }



        return new GameLaunchResult

        {

            Process = process,

            EarlyError = earlyError

        };

    }



    private static string ReadTail(string text)

    {

        var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0)

        {

            return string.Empty;

        }



        return string.Join(Environment.NewLine, lines.TakeLast(Math.Min(8, lines.Length)));

    }



    private static string Quote(string value) => $"\"{value.Replace("\"", "\\\"")}\"";

    private static void EnsureClientWindowIcon()
    {
        var source = Path.Combine(AppContext.BaseDirectory, "Assets", "STRIcon.png");
        if (!File.Exists(source))
        {
            return;
        }

        var targets = new[]
        {
            Path.Combine(LauncherConstants.ClientDataDirectory, "STRIcon.png"),
            Path.Combine(LauncherConstants.ClientDataDirectory, "ClassicUO.png"),
            Path.Combine(LauncherConstants.ClientBinaryDirectory, "STRIcon.png"),
            Path.Combine(LauncherConstants.ClientBinaryDirectory, "cuo.png"),
            Path.Combine(LauncherConstants.ClientBinaryDirectory, "ClassicUO.png"),
        };

        foreach (var target in targets)
        {
            try
            {
                var dir = Path.GetDirectoryName(target);
                if (!string.IsNullOrEmpty(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                File.Copy(source, target, overwrite: true);
            }
            catch
            {
                // Best effort — client also checks settings folder directly.
            }
        }
    }

}


