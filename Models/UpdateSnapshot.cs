namespace SkythornLauncher.Models;

public sealed class UpdateSnapshot
{
    public static UpdateSnapshot Checking { get; } = new() { State = UpdateCheckState.Checking };

    public UpdateCheckState State { get; init; }
    public string CurrentVersion { get; init; } = LauncherVersionInfo.Display;
    public string? LatestVersion { get; init; }
    public string? ReleaseTag { get; init; }
    public int OutdatedFileCount { get; init; }
    public IReadOnlyList<string> OutdatedPaths { get; init; } = Array.Empty<string>();
    public DateTime? CheckedUtc { get; init; }
    public string? ErrorMessage { get; init; }

    public static UpdateSnapshot UpToDate(UpdateManifest manifest, DateTime checkedUtc) =>
        new()
        {
            State = UpdateCheckState.UpToDate,
            LatestVersion = manifest.Version,
            ReleaseTag = manifest.ReleaseTag,
            OutdatedFileCount = 0,
            OutdatedPaths = Array.Empty<string>(),
            CheckedUtc = checkedUtc
        };

    public static UpdateSnapshot UpdateAvailable(
        UpdateManifest manifest,
        IReadOnlyList<string> outdatedPaths,
        DateTime checkedUtc,
        bool gameRunning) =>
        new()
        {
            State = UpdateCheckState.UpdateAvailable,
            LatestVersion = manifest.Version,
            ReleaseTag = manifest.ReleaseTag,
            OutdatedFileCount = outdatedPaths.Count,
            OutdatedPaths = outdatedPaths,
            CheckedUtc = checkedUtc,
            ErrorMessage = gameRunning ? "Please close the game before updating." : null
        };

    public static UpdateSnapshot CheckFailed(DateTime checkedUtc, string? message = null) =>
        new()
        {
            State = UpdateCheckState.CheckFailed,
            CheckedUtc = checkedUtc,
            ErrorMessage = message
        };

    public static UpdateSnapshot Progress(UpdateCheckState state, UpdateManifest? manifest = null, string? errorMessage = null) =>
        new()
        {
            State = state,
            LatestVersion = manifest?.Version,
            ReleaseTag = manifest?.ReleaseTag,
            ErrorMessage = errorMessage
        };

    public static UpdateSnapshot UpdateFailed(string message, UpdateManifest? manifest = null) =>
        new()
        {
            State = UpdateCheckState.UpdateFailed,
            LatestVersion = manifest?.Version,
            ReleaseTag = manifest?.ReleaseTag,
            ErrorMessage = message
        };

    public bool IsBusy =>
        State is UpdateCheckState.Checking
            or UpdateCheckState.Downloading
            or UpdateCheckState.Verifying
            or UpdateCheckState.RestartingLauncher;
}

public enum UpdateCheckState
{
    Checking,
    UpToDate,
    UpdateAvailable,
    CheckFailed,
    Downloading,
    Verifying,
    RestartingLauncher,
    UpdateFailed
}
