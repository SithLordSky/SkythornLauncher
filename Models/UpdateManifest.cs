using System.Text.Json.Serialization;

namespace SkythornLauncher.Models;

public sealed class UpdateManifest
{
    [JsonPropertyName("version")]
    public string Version { get; init; } = string.Empty;

    [JsonPropertyName("releaseTag")]
    public string ReleaseTag { get; init; } = string.Empty;

    [JsonPropertyName("publishedUtc")]
    public string PublishedUtc { get; init; } = string.Empty;

    [JsonPropertyName("releaseNotes")]
    public string ReleaseNotes { get; init; } = string.Empty;

    [JsonPropertyName("files")]
    public IReadOnlyList<UpdateManifestFile> Files { get; init; } = Array.Empty<UpdateManifestFile>();
}

public sealed class UpdateManifestFile
{
    [JsonPropertyName("path")]
    public string Path { get; init; } = string.Empty;

    [JsonPropertyName("size")]
    public long Size { get; init; }

    [JsonPropertyName("sha256")]
    public string Sha256 { get; init; } = string.Empty;

    /// <summary>Flat GitHub Release asset name. When omitted, derived as path with '/' replaced by '__'.</summary>
    [JsonPropertyName("assetName")]
    public string AssetName { get; init; } = string.Empty;
}
