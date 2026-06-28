# Publishes the player launcher only. Server sidecar lives in D:\LBRServerStatus.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$stage = Join-Path $root ".publish"

Stop-Process -Name SkythornLauncher -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

if (Test-Path $stage) {
    Remove-Item $stage -Recurse -Force
}

dotnet publish (Join-Path $root "SkythornLauncher.csproj") -c Release -r win-x64 -o $stage

dotnet publish (Join-Path $root "SkythornUpdater\SkythornUpdater.csproj") -c Release -r win-x64 -o $stage

Get-ChildItem $stage -File | ForEach-Object { Copy-Item $_.FullName $root -Force }

# Copy Assets except user-owned menu backgrounds (never overwrite those during publish).
$assetsStage = Join-Path $stage "Assets"
$assetsRoot = Join-Path $root "Assets"
$protectedBackgrounds = @("profiles-background.png", "settings-background.png")
if (Test-Path $assetsStage) {
    Get-ChildItem $assetsStage -Recurse -File | ForEach-Object {
        $relative = $_.FullName.Substring($assetsStage.Length).TrimStart('\', '/')
        $fileName = Split-Path $relative -Leaf
        $dest = Join-Path $assetsRoot $relative
        if ($protectedBackgrounds -contains $fileName -and (Test-Path $dest)) {
            Write-Host "Preserved $relative"
            return
        }

        $destDir = Split-Path $dest -Parent
        if (-not (Test-Path $destDir)) {
            New-Item -ItemType Directory -Path $destDir -Force | Out-Null
        }

        Copy-Item $_.FullName $dest -Force
    }
}

Remove-Item $stage -Recurse -Force

# Player install must not include server-sidecar artifacts.
foreach ($legacy in @("StatusServer", "ServerPack", "ShardStatusServer")) {
    $legacyPath = Join-Path $root $legacy
    if (Test-Path $legacyPath) {
        Remove-Item $legacyPath -Recurse -Force
        Write-Host "Removed $legacy from player install"
    }
}

# Clean legacy / build clutter (keep source, Assets, ClassicUO, Client)
$cleanPaths = @(
    (Join-Path $root "bin"),
    (Join-Path $root ".publish")
)
foreach ($path in $cleanPaths) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
    }
}

Get-ChildItem $root -Filter "*.pdb" -File -ErrorAction SilentlyContinue | Remove-Item -Force

# Generate update-manifest.json for GitHub Release upload (Phase 1 updater).
$manifestPathsFile = Join-Path $root "update-manifest-paths.txt"
$releaseDir = Join-Path $root ".release"
if (-not (Test-Path $releaseDir)) {
    New-Item -ItemType Directory -Path $releaseDir -Force | Out-Null
}

$verCs = Get-Content (Join-Path $root "LauncherVersionInfo.cs") -Raw
$major = if ($verCs -match 'Major\s*=\s*(\d+)') { [int]$matches[1] } else { throw "Could not read Major from LauncherVersionInfo.cs" }
$minor = if ($verCs -match 'Minor\s*=\s*(\d+)') { [int]$matches[1] } else { throw "Could not read Minor from LauncherVersionInfo.cs" }
$patch = if ($verCs -match 'Patch\s*=\s*(\d+)') { [int]$matches[1] } else { throw "Could not read Patch from LauncherVersionInfo.cs" }
$displayVersion = "$major.$minor.$patch"
$releaseTag = "v$displayVersion"

$manifestEntries = @()
$releaseAssetsDir = Join-Path $releaseDir "assets"
if (Test-Path $releaseAssetsDir) {
    Remove-Item $releaseAssetsDir -Recurse -Force
}
New-Item -ItemType Directory -Path $releaseAssetsDir -Force | Out-Null

foreach ($rel in (Get-Content $manifestPathsFile | Where-Object { $_ -and -not $_.StartsWith('#') })) {
    $rel = $rel.Trim()
    $full = Join-Path $root $rel
    if (-not (Test-Path $full)) {
        throw "Missing manifest file: $rel"
    }

    $normalizedPath = ($rel -replace '\\', '/')
    $assetName = $normalizedPath -replace '/', '__'
    $item = Get-Item $full
    $hash = (Get-FileHash $full -Algorithm SHA256).Hash.ToLowerInvariant()
    Copy-Item $full (Join-Path $releaseAssetsDir $assetName) -Force
    $manifestEntries += [ordered]@{
        path      = $normalizedPath
        assetName = $assetName
        size      = $item.Length
        sha256    = $hash
    }
}

$releaseNotes = @"
Skythorn Launcher v$displayVersion

- Settings: renamed Footstep volume slider to Music Volume; syncs login and in-game music volume.
- Profiles: added Profile Name field so profiles can be renamed on Save.
"@.Trim()

$manifest = [ordered]@{
    version      = $displayVersion
    releaseTag   = $releaseTag
    publishedUtc = (Get-Date).ToUniversalTime().ToString('o')
    releaseNotes = $releaseNotes
    files        = $manifestEntries
}

$manifestPath = Join-Path $releaseDir "update-manifest.json"
$manifest | ConvertTo-Json -Depth 5 | Set-Content -Path $manifestPath -Encoding UTF8
Copy-Item $manifestPath (Join-Path $releaseAssetsDir "update-manifest.json") -Force
Write-Host "Wrote $manifestPath ($($manifestEntries.Count) files) for GitHub Release $releaseTag"
Write-Host "Release assets folder: $releaseAssetsDir (upload manifest + assets to GitHub Release)"

# Verify install root matches the manifest we just generated (prevents git/release drift).
$verifyScript = Join-Path $root "verify-install-against-manifest.ps1"
if (Test-Path $verifyScript) {
    & $verifyScript -ManifestPath $manifestPath -InstallRoot $root
    if ($LASTEXITCODE -ne 0) {
        throw "Install verification failed. Do not upload this release until local files match the manifest."
    }
}

Write-Host "Published SkythornLauncher.exe to $root (player build only)"
Write-Host "IMPORTANT: Commit the published binaries to git before uploading GitHub Release $releaseTag, or git clones will always show an update."
