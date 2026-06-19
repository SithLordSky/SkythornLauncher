using SkythornLauncher.Models;

namespace SkythornLauncher.Services;

internal static class UpdateAssetNaming
{
    /// <summary>
    /// Maps manifest install path to flat GitHub Release asset name.
    /// Example: Client/cuo.dll → Client__cuo.dll
    /// </summary>
    public static string ToAssetName(string manifestPath)
    {
        return manifestPath
            .Replace('\\', '/')
            .Replace("/", "__");
    }

    public static string ResolveAssetName(UpdateManifestFile entry)
    {
        if (!string.IsNullOrWhiteSpace(entry.AssetName))
        {
            return entry.AssetName;
        }

        return ToAssetName(entry.Path);
    }
}
