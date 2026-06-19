using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

public sealed class UpdateService : IDisposable
{
    private const string ManifestAssetName = "update-manifest.json";

    private readonly HttpClient _http = new()
    {
        Timeout = TimeSpan.FromSeconds(15)
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
            var manifest = await FetchLatestManifestAsync(cancellationToken);
            if (manifest == null || manifest.Files.Count == 0)
            {
                Publish(UpdateSnapshot.CheckFailed(checkedUtc));
                return;
            }

            var outdatedCount = CountOutdatedFiles(manifest);
            Publish(outdatedCount > 0
                ? UpdateSnapshot.UpdateAvailable(manifest, outdatedCount, checkedUtc)
                : UpdateSnapshot.UpToDate(manifest, checkedUtc));
        }
        catch
        {
            Publish(UpdateSnapshot.CheckFailed(checkedUtc));
        }
    }

    private void Publish(UpdateSnapshot snapshot)
    {
        Latest = snapshot;
        StatusUpdated?.Invoke(snapshot);
    }

    private async Task<UpdateManifest?> FetchLatestManifestAsync(CancellationToken cancellationToken)
    {
        using var releaseResponse = await _http.GetAsync(LauncherConstants.GitHubLatestReleaseApiUrl, cancellationToken);
        releaseResponse.EnsureSuccessStatusCode();

        await using var releaseStream = await releaseResponse.Content.ReadAsStreamAsync(cancellationToken);
        var release = await JsonSerializer.DeserializeAsync<GitHubReleaseResponse>(releaseStream, JsonOptions, cancellationToken);
        var assetUrl = release?.Assets?
            .FirstOrDefault(a => string.Equals(a.Name, ManifestAssetName, StringComparison.OrdinalIgnoreCase))
            ?.BrowserDownloadUrl;

        if (string.IsNullOrWhiteSpace(assetUrl))
        {
            return null;
        }

        using var manifestResponse = await _http.GetAsync(assetUrl, cancellationToken);
        manifestResponse.EnsureSuccessStatusCode();

        await using var manifestStream = await manifestResponse.Content.ReadAsStreamAsync(cancellationToken);
        return await JsonSerializer.DeserializeAsync<UpdateManifest>(manifestStream, JsonOptions, cancellationToken);
    }

    private static int CountOutdatedFiles(UpdateManifest manifest)
    {
        var root = LauncherConstants.InstallRoot;
        var outdated = 0;

        foreach (var entry in manifest.Files)
        {
            if (string.IsNullOrWhiteSpace(entry.Path))
            {
                outdated++;
                continue;
            }

            var localPath = Path.Combine(root, entry.Path.Replace('/', Path.DirectorySeparatorChar));
            if (!File.Exists(localPath))
            {
                outdated++;
                continue;
            }

            var info = new FileInfo(localPath);
            if (info.Length != entry.Size)
            {
                outdated++;
                continue;
            }

            var hash = ComputeSha256(localPath);
            if (!string.Equals(hash, entry.Sha256, StringComparison.OrdinalIgnoreCase))
            {
                outdated++;
            }
        }

        return outdated;
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
        PropertyNameCaseInsensitive = true
    };

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
