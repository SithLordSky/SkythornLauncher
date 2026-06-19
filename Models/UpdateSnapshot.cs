namespace SkythornLauncher.Models;

public sealed class UpdateSnapshot
{
    public static UpdateSnapshot Checking { get; } = new() { State = UpdateCheckState.Checking };

    public UpdateCheckState State { get; init; }
    public string CurrentVersion { get; init; } = LauncherVersionInfo.Display;
    public string? LatestVersion { get; init; }
    public string? ReleaseTag { get; init; }
    public int OutdatedFileCount { get; init; }
    public DateTime? CheckedUtc { get; init; }

    public static UpdateSnapshot UpToDate(UpdateManifest manifest, DateTime checkedUtc) =>
        new()
        {
            State = UpdateCheckState.UpToDate,
            LatestVersion = manifest.Version,
            ReleaseTag = manifest.ReleaseTag,
            OutdatedFileCount = 0,
            CheckedUtc = checkedUtc
        };

    public static UpdateSnapshot UpdateAvailable(UpdateManifest manifest, int outdatedCount, DateTime checkedUtc) =>
        new()
        {
            State = UpdateCheckState.UpdateAvailable,
            LatestVersion = manifest.Version,
            ReleaseTag = manifest.ReleaseTag,
            OutdatedFileCount = outdatedCount,
            CheckedUtc = checkedUtc
        };

    public static UpdateSnapshot CheckFailed(DateTime checkedUtc) =>
        new()
        {
            State = UpdateCheckState.CheckFailed,
            CheckedUtc = checkedUtc
        };
}

public enum UpdateCheckState
{
    Checking,
    UpToDate,
    UpdateAvailable,
    CheckFailed
}
