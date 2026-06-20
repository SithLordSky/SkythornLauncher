using System.Diagnostics;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Windows;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

public sealed class UpdateService : IDisposable
{
    private const string ManifestAssetName = "update-manifest.json";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromMinutes(10)
    };

    public event Action<UpdateSnapshot>? StatusUpdated;

    public UpdateSnapshot Latest { get; private set; } = UpdateSnapshot.Checking;

    public UpdateService()
    {
        _http.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("SkythornLauncher", LauncherVersionInfo.Display));
        _http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/vnd.github+json"));
    }

    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        Publish(UpdateSnapshot.Checking);

        var checkedUtc = DateTime.UtcNow;

        try
        {
            var release = await FetchLatestReleaseAsync(cancellationToken);
            if (release?.Manifest == null || release.Manifest.Files.Count == 0)
            {
                Publish(UpdateSnapshot.CheckFailed(checkedUtc, "Update manifest was not found on the latest release."));
                return;
            }

            var outdated = GetOutdatedFiles(release.Manifest);
            var outdatedPaths = outdated
                .Select(entry => entry.Path.Replace('\\', '/'))
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .ToList();
            Publish(outdatedPaths.Count > 0
                ? UpdateSnapshot.UpdateAvailable(
                    release.Manifest,
                    outdatedPaths,
                    checkedUtc,
                    GameProcessTracker.IsGameRunning())
                : UpdateSnapshot.UpToDate(release.Manifest, checkedUtc));
        }
        catch (Exception ex)
        {
            Publish(UpdateSnapshot.CheckFailed(checkedUtc, ex.Message));
        }
    }

    public async Task InstallUpdateAsync(CancellationToken cancellationToken = default)
    {
        UpdateManifest? manifest = null;

        try
        {
            if (GameProcessTracker.IsGameRunning())
            {
                Publish(UpdateSnapshot.UpdateFailed("Please close the game before updating."));
                return;
            }

            Publish(UpdateSnapshot.Progress(UpdateCheckState.Downloading));

            var release = await FetchLatestReleaseAsync(cancellationToken);
            manifest = release?.Manifest;
            if (manifest == null || release?.Assets == null)
            {
                Publish(UpdateSnapshot.UpdateFailed("Update manifest was not found on the latest release.", manifest));
                return;
            }

            var outdated = GetOutdatedFiles(manifest);
            if (outdated.Count == 0)
            {
                Publish(UpdateSnapshot.UpToDate(manifest, DateTime.UtcNow));
                return;
            }

            var stagingDir = Path.Combine(
                LauncherConstants.UpdateStagingRoot,
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(stagingDir);

            foreach (var entry in outdated)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var assetName = UpdateAssetNaming.ResolveAssetName(entry);
                if (!release.Assets.TryGetValue(assetName, out var assetUrl))
                {
                    throw new InvalidOperationException($"Release asset not found: {assetName}");
                }

                var stagedPath = Path.Combine(stagingDir, assetName);
                await DownloadFileAsync(assetUrl, stagedPath, cancellationToken);
            }

            Publish(UpdateSnapshot.Progress(UpdateCheckState.Verifying, manifest));

            foreach (var entry in outdated)
            {
                var assetName = UpdateAssetNaming.ResolveAssetName(entry);
                var stagedPath = Path.Combine(stagingDir, assetName);
                VerifyStagedFile(stagedPath, entry);
            }

            if (GameProcessTracker.IsGameRunning())
            {
                Publish(UpdateSnapshot.UpdateFailed("Please close the game before applying the update.", manifest));
                return;
            }

            if (!File.Exists(LauncherConstants.UpdaterExePath))
            {
                throw new FileNotFoundException(
                    $"Updater helper is missing: {LauncherConstants.UpdaterExePath}");
            }

            var backupDir = Path.Combine(
                LauncherConstants.UpdateBackupRoot,
                DateTime.UtcNow.ToString("yyyyMMdd_HHmmss"));
            Directory.CreateDirectory(backupDir);

            var job = new UpdateJob
            {
                InstallRoot = LauncherConstants.InstallRoot,
                LauncherExe = LauncherConstants.LauncherExePath,
                WaitPid = Environment.ProcessId,
                BackupDir = backupDir,
                StagingDir = stagingDir,
                Files = outdated.Select(entry => new UpdateJobFile
                {
                    Path = entry.Path.Replace('\\', '/'),
                    Source = Path.Combine(stagingDir, UpdateAssetNaming.ResolveAssetName(entry))
                }).ToList()
            };

            var jobPath = Path.Combine(stagingDir, "update-job.json");
            await File.WriteAllTextAsync(
                jobPath,
                JsonSerializer.Serialize(job, JsonOptions),
                cancellationToken);

            Publish(UpdateSnapshot.Progress(UpdateCheckState.RestartingLauncher, manifest));

            Process.Start(new ProcessStartInfo
            {
                FileName = LauncherConstants.UpdaterExePath,
                Arguments = $"--job \"{jobPath}\"",
                WorkingDirectory = LauncherConstants.InstallRoot,
                UseShellExecute = false
            });

            Application.Current.Shutdown();
        }
        catch (Exception ex)
        {
            Publish(UpdateSnapshot.UpdateFailed(ex.Message, manifest));
        }
    }

    private void Publish(UpdateSnapshot snapshot)
    {
        Latest = snapshot;
        StatusUpdated?.Invoke(snapshot);
    }

    private async Task<ReleaseBundle?> FetchLatestReleaseAsync(CancellationToken cancellationToken)
    {
        using var releaseResponse = await _http.GetAsync(LauncherConstants.GitHubLatestReleaseApiUrl, cancellationToken);
        releaseResponse.EnsureSuccessStatusCode();

        await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(releaseStream, JsonOptions, cancellationToken);
        if (release?.Assets == null)
        {
            return null;
        }

        var assets = release.Assets
            .Where(a => !string.IsNullOrWhiteSpace(a.Name) && !string.IsNullOrWhiteSpace(a.BrowserDownloadUrl))
            .GroupBy(a => a.Name!, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First().BrowserDownloadUrl!, StringComparer.OrdinalIgnoreCase);

        if (!assets.TryGetValue(ManifestAssetName, out var manifestUrl))
        {
            return new ReleaseBundle(null, assets);
        }

        using var manifestResponse = await _http.GetAsync(manifestUrl, cancellationToken);
        manifestResponse.EnsureSuccessStatusCode();

        await using var manifestStream = await manifestResponse.Content.ReadAsStreamAsync(cancellationToken);
        var manifest = await JsonSerializer.DeserializeAsync<UpdateManifest>(manifestStream, JsonOptions, cancellationToken);
        return new ReleaseBundle(manifest, assets);
    }

    private async Task DownloadFileAsync(string url, string destinationPath, CancellationToken cancellationToken)
    {
        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        using var response = await _http.GetAsync(url, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        await using var file = File.Create(destinationPath);
        await stream.CopyToAsync(file, cancellationToken);
    }

    private static void VerifyStagedFile(string stagedPath, UpdateManifestFile entry)
    {
        if (!File.Exists(stagedPath))
        {
            throw new InvalidOperationException($"Downloaded file is missing: {entry.Path}");
        }

        var info = new FileInfo(stagedPath);
        if (info.Length != entry.Size)
        {
            throw new InvalidOperationException($"Downloaded file size mismatch for {entry.Path}.");
        }

        var hash = ComputeSha256(stagedPath);
        if (!string.Equals(hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Downloaded file hash mismatch for {entry.Path}.");
        }
    }

    private static List<UpdateManifestFile> GetOutdatedFiles(UpdateManifest manifest)
    {
        var root = LauncherConstants.InstallRoot;
        var outdated = new List<UpdateManifestFile>();

        foreach (var entry in manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                outdated.Add(entry);
                continue;
            }

            if (!IsManifestPathAllowed(entry.Path))
            {
                continue;
            }

            var localPath = Path.Combine(root, entry.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(localPath))
            {
                outdated.Add(entry);
                continue;
            }

            var info = new FileInfo(localPath);
            if (info.Length != entry.Size)
            {
                outdated.Add(entry);
                continue;
            }

            var hash = ComputeSha256(localPath);
            if (!string.Equals(hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                outdated.Add(entry);
            }
        }

        return outdated;
    }

    internal static bool IsManifestPathAllowed(string manifestPath)
    {
        var normalized = manifestPath.Replace('\\', '/').Trim();
        if (string.IsNullOrWhiteSpace(normalized) ||
            normalized.Contains("..", StringComparison.Ordinal))
        {
            return false;
        }

        if (normalized.StartsWith("ClassicUO/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalized.StartsWith("Client/Data/", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        if (normalized.Equals("Assets/profiles-background.png", StringComparison.OrdinalIgnoreCase) ||
            normalized.Equals("Assets/settings-background.png", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    private static string ComputeSha256(string path)
    {
        using var stream = File.OpenRead(path);
        var hash = SHA256.HashData(stream);
        return Convert.ToHexString(hash).ToLowerInvariant();
    }

    public void Dispose() => _http.Dispose();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    private sealed record ReleaseBundle(UpdateManifest? Manifest, IReadOnlyDictionary<string, string> Assets);

    private sealed class GitHubReleaseResponse
    {
        [JsonPropertyName("tag_name")]
        public string? TagName { get; set; }

        [JsonPropertyName("assets")]
        public List<GitHubReleaseAsset>? Assets { get; set; }
    }

    private sealed class GitHubReleaseAsset
    {
        [JsonPropertyName("name")]
        public string? Name { get; set; }

        [JsonPropertyName("browser_download_url")]
        public string? BrowserDownloadUrl { get; set; }
    }
}
