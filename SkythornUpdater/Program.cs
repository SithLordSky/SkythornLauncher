using System.Diagnostics;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;

if (!TryGetJobPath(args, out var jobPath))
{
    Console.Error.WriteLine("Usage: SkythornUpdater --job <path-to-update-job.json>");
    return 1;
}

UpdateJob? job;
try
{
    job = JsonSerializer.Deserialize<UpdateJob>(
        await File.ReadAllTextAsync(jobPath),
        JobJsonOptions.Value);
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Failed to read update job: {ex.Message}");
    return 1;
}

if (job == null || job.Files.Count == 0)
{
    Console.Error.WriteLine("Update job is empty or invalid.");
    return 1;
}

try
{
    WaitForProcessExit(job.WaitPid);

    foreach (var file in job.Files)
    {
        ApplyFile(job, file);
    }

    TryDeleteDirectory(job.StagingDir);

    Process.Start(new ProcessStartInfo
    {
        FileName = job.LauncherExe,
        WorkingDirectory = job.InstallRoot,
        UseShellExecute = true
    });

    return 0;
}
catch (Exception ex)
{
    Console.Error.WriteLine($"Update failed: {ex.Message}");
    return 1;
}

static bool TryGetJobPath(string[] args, out string jobPath)
{
    jobPath = string.Empty;
    for (var i = 0; i < args.Length - 1; i++)
    {
        if (string.Equals(args[i], "--job", StringComparison.OrdinalIgnoreCase))
        {
            jobPath = args[i + 1].Trim('"');
            return File.Exists(jobPath);
        }
    }

    return false;
}

static void WaitForProcessExit(int pid)
{
    if (pid <= 0)
    {
        return;
    }

    try
    {
        var process = Process.GetProcessById(pid);
        process.WaitForExit(60_000);
    }
    catch
    {
        Thread.Sleep(1500);
    }
}

static void ApplyFile(UpdateJob job, UpdateJobFile file)
{
    var relativePath = file.Path.Replace('/', Path.DirectorySeparatorChar);
    var targetPath = Path.GetFullPath(Path.Combine(job.InstallRoot, relativePath));
    var installRoot = Path.GetFullPath(job.InstallRoot);

    if (!targetPath.StartsWith(installRoot, StringComparison.OrdinalIgnoreCase))
    {
        throw new InvalidOperationException($"Refusing to write outside install root: {file.Path}");
    }

    if (!File.Exists(file.Source))
    {
        throw new FileNotFoundException($"Staged file missing: {file.Source}");
    }

    var targetDirectory = Path.GetDirectoryName(targetPath);
    if (!string.IsNullOrEmpty(targetDirectory))
    {
        Directory.CreateDirectory(targetDirectory);
    }

    if (File.Exists(targetPath))
    {
        var backupPath = Path.Combine(job.BackupDir, relativePath);
        var backupDirectory = Path.GetDirectoryName(backupPath);
        if (!string.IsNullOrEmpty(backupDirectory))
        {
            Directory.CreateDirectory(backupDirectory);
        }

        File.Copy(targetPath, backupPath, overwrite: true);
    }

    File.Copy(file.Source, targetPath, overwrite: true);
}

static void TryDeleteDirectory(string path)
{
    try
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
    catch
    {
        // Best-effort cleanup only.
    }
}

internal sealed class UpdateJob
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

internal sealed class UpdateJobFile
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("source")]
    public string Source { get; init; } = string.Empty;
}

static class JobJsonOptions
{
    public static readonly JsonSerializerOptions Value = new()
    {
        PropertyNameCaseInsensitive = true
    };
}
