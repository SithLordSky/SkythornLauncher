using System.Diagnostics;
using System.Text;
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

var exitCode = 0;
Exception? failure = null;

try
{
    WaitForProcessExit(job.WaitPid);

    foreach (var file in job.Files)
    {
        ApplyFile(job, file);
    }

    TryDeleteDirectory(job.StagingDir);
}
catch (Exception ex)
{
    failure = ex;
    exitCode = 1;
    Console.Error.WriteLine($"Update failed: {ex.Message}");
    WriteFailureLog(job, ex);
}

finally
{
    TryLaunchLauncher(job, failure != null);
}

return exitCode;

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
    if (!IsPathWithinInstallRoot(job.InstallRoot, relativePath, out var targetPath))
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

static bool IsPathWithinInstallRoot(string installRoot, string relativePath, out string targetPath)
{
    targetPath = Path.GetFullPath(Path.Combine(installRoot, relativePath));
    var normalizedRoot = NormalizeInstallRoot(installRoot);
    var fullInstallRoot = Path.GetFullPath(installRoot);

    if (string.Equals(targetPath, fullInstallRoot, StringComparison.OrdinalIgnoreCase))
    {
        return true;
    }

    return targetPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
}

static string NormalizeInstallRoot(string installRoot)
{
    var fullInstallRoot = Path.GetFullPath(installRoot);
    if (!fullInstallRoot.EndsWith(Path.DirectorySeparatorChar))
    {
        fullInstallRoot += Path.DirectorySeparatorChar;
    }

    return fullInstallRoot;
}

static void WriteFailureLog(UpdateJob job, Exception ex)
{
    var logPath = Path.Combine(job.StagingDir, "update-failure.log");
    if (string.IsNullOrWhiteSpace(job.StagingDir) || !Directory.Exists(job.StagingDir))
    {
        logPath = Path.Combine(job.BackupDir, "update-failure.log");
    }

    try
    {
        var directory = Path.GetDirectoryName(logPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var builder = new StringBuilder();
        builder.AppendLine(DateTime.UtcNow.ToString("o"));
        builder.AppendLine(ex.ToString());
        File.WriteAllText(logPath, builder.ToString());
    }
    catch
    {
        // Best-effort logging only.
    }
}

static void TryLaunchLauncher(UpdateJob job, bool updateFailed)
{
    if (string.IsNullOrWhiteSpace(job.LauncherExe) || !File.Exists(job.LauncherExe))
    {
        return;
    }

    try
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = job.LauncherExe,
            WorkingDirectory = job.InstallRoot,
            UseShellExecute = true
        });
    }
    catch (Exception ex)
    {
        Console.Error.WriteLine(updateFailed
            ? $"Update failed and launcher could not be restarted: {ex.Message}"
            : $"Launcher could not be restarted: {ex.Message}");
    }
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
