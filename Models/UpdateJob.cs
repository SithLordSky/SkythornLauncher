using System.Text.Json.Serialization;

namespace SkythornLauncher.Models;

public sealed class UpdateJob
{
    [JsonPropertyName("installRoot")]
    public string InstallRoot { get; init; } = string.Empty;

    [JsonPropertyName("launcherExe")]
    public string LauncherExe { get; init; } = string.Empty;

    [JsonPropertyName("waitPid")]
    public int WaitPid { get; init; }

    [JsonPropertyName("backupDir")]
    public string BackupDir { get; init; } = string.Empty;

    [JsonPropertyName("stagingDir")]
    public string StagingDir { get; init; } = string.Empty;

    [JsonPropertyName("files")]
    public IReadOnlyList<UpdateJobFile> Files { get; init; } = Array.Empty<UpdateJobFile>();
}

public sealed class UpdateJobFile
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;
}
