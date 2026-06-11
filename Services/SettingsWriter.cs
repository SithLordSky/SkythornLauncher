using System.Text.Json;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal static class SettingsWriter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static void Write(LauncherProfile profile, string? password = null)
    {
        var settingsPath = LauncherConstants.SettingsJsonPath;
        var settingsDir = Path.GetDirectoryName(settingsPath);

        if (!string.IsNullOrEmpty(settingsDir) && !Directory.Exists(settingsDir))
        {
            Directory.CreateDirectory(settingsDir);
        }

        var settings = LoadDictionary(settingsPath);
        var prefs = profile.Preferences;

        settings["ip"] = LauncherConstants.ServerIp;
        settings["port"] = LauncherConstants.ServerPort;
        settings["clientversion"] = LauncherConstants.ClientVersion;
        settings["last_server_name"] = LauncherConstants.ShardName;
        settings["ultimaonlinedirectory"] = profile.UltimaOnlineDirectory;
        settings["saveaccount"] = profile.SaveAccount || prefs.SaveAccount;
        settings["username"] = profile.SaveAccount || prefs.SaveAccount ? profile.Username : string.Empty;
        settings["password"] = string.Empty;
        settings["autologin"] = prefs.AutoLogin || prefs.AutoLoginOnPlay;
        settings["reconnect"] = prefs.AutoReconnect;
        settings["reconnect_time"] = Math.Max(1000, prefs.ReconnectDelayMs);
        settings["login_music"] = prefs.EnableMusic;
        settings["login_music_volume"] = Math.Clamp(prefs.MusicVolume, 0, 100);
        settings["footsteps_volume"] = Math.Clamp(prefs.FootstepsVolume, 0, 100);
        settings["force_driver"] = prefs.ForceDriver;

        if (!settings.ContainsKey("lang"))
        {
            settings["lang"] = "IVL";
        }

        if (!settings.ContainsKey("fps"))
        {
            settings["fps"] = 60;
        }

        File.WriteAllText(settingsPath, JsonSerializer.Serialize(settings, JsonOptions));
    }

    public static ClientPreferences ReadPreferences()
    {
        var settings = LoadDictionary(LauncherConstants.SettingsJsonPath);
        var prefs = new ClientPreferences();

        if (settings.TryGetValue("autologin", out var autoLogin))
        {
            prefs.AutoLogin = ReadBool(autoLogin);
            prefs.AutoLoginOnPlay = prefs.AutoLogin;
        }

        if (settings.TryGetValue("reconnect", out var reconnect))
        {
            prefs.AutoReconnect = ReadBool(reconnect);
        }

        if (settings.TryGetValue("reconnect_time", out var reconnectTime) && TryReadInt(reconnectTime, out var ms))
        {
            prefs.ReconnectDelayMs = ms;
        }

        if (settings.TryGetValue("saveaccount", out var saveAccount))
        {
            prefs.SaveAccount = ReadBool(saveAccount);
        }

        if (settings.TryGetValue("login_music", out var music))
        {
            prefs.EnableMusic = ReadBool(music);
        }

        if (settings.TryGetValue("login_music_volume", out var volume) && TryReadInt(volume, out var vol))
        {
            prefs.MusicVolume = vol;
        }

        if (settings.TryGetValue("footsteps_volume", out var footstepsVolume) && TryReadInt(footstepsVolume, out var footVol))
        {
            prefs.FootstepsVolume = footVol;
        }
        else if (settings.TryGetValue("default_footsteps_volume", out var legacyFootstepsVolume) && TryReadInt(legacyFootstepsVolume, out var legacyFootVol))
        {
            prefs.FootstepsVolume = legacyFootVol;
        }

        if (settings.TryGetValue("force_driver", out var driver) && TryReadInt(driver, out var drv))
        {
            prefs.ForceDriver = (byte)Math.Clamp(drv, 0, 2);
        }

        return prefs;
    }

    public static void ApplyPreferences(ClientPreferences prefs)
    {
        var profile = new LauncherProfile { Preferences = prefs };
        Write(profile);
    }

    private static Dictionary<string, object?> LoadDictionary(string settingsPath)
    {
        if (!File.Exists(settingsPath))
        {
            return new Dictionary<string, object?>();
        }

        try
        {
            return JsonSerializer.Deserialize<Dictionary<string, object?>>(File.ReadAllText(settingsPath)) ?? new();
        }
        catch
        {
            return new Dictionary<string, object?>();
        }
    }

    private static bool ReadBool(object? value)
    {
        return value switch
        {
            bool b => b,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            string s => bool.TryParse(s, out var parsed) && parsed,
            _ => false
        };
    }

    private static bool TryReadInt(object? value, out int result)
    {
        result = 0;
        switch (value)
        {
            case int i:
                result = i;
                return true;
            case JsonElement el when el.TryGetInt32(out var j):
                result = j;
                return true;
            case string s when int.TryParse(s, out var parsed):
                result = parsed;
                return true;
            default:
                return false;
        }
    }
}
