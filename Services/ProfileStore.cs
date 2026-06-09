using System.Text.Json;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal sealed class ProfileStore
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public LauncherState Load()
    {
        var path = LauncherConstants.ProfilesPath;
        EnsureDirectory(path);

        if (!File.Exists(path))
        {
            return CreateDefaultState();
        }

        try
        {
            var json = File.ReadAllText(path);
            var state = JsonSerializer.Deserialize<LauncherState>(json, JsonOptions);
            return state ?? CreateDefaultState();
        }
        catch
        {
            return CreateDefaultState();
        }
    }

    public void Save(LauncherState state)
    {
        var path = LauncherConstants.ProfilesPath;
        EnsureDirectory(path);
        var json = JsonSerializer.Serialize(state, JsonOptions);
        File.WriteAllText(path, json);
    }

    private static LauncherState CreateDefaultState()
    {
        return new LauncherState
        {
            ActiveProfileName = "Default",
            Profiles =
            [
                new LauncherProfile
                {
                    Name = "Default",
                    UltimaOnlineDirectory = LauncherConstants.DefaultUltimaOnlinePath,
                    Preferences = SettingsWriter.ReadPreferences()
                }
            ]
        };
    }

    private static void EnsureDirectory(string filePath)
    {
        var dir = Path.GetDirectoryName(filePath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }
    }
}
