param(
    [Parameter(Mandatory = $true)]
    [string]$ManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$InstallRoot
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $ManifestPath)) {
    Write-Error "Manifest not found: $ManifestPath"
}

$manifest = Get-Content $ManifestPath -Raw | ConvertFrom-Json
if ($null -eq $manifest.files -or $manifest.files.Count -eq 0) {
    Write-Error "Manifest has no files: $ManifestPath"
}

$outdated = @()
foreach ($entry in $manifest.files) {
    $relative = [string]$entry.path
    if ([string]::IsNullOrWhiteSpace($relative)) {
        $outdated += "(empty path entry)"
        continue
    }

    if ($relative.StartsWith("ClassicUO/", [System.StringComparison]::OrdinalIgnoreCase) -or
        $relative.StartsWith("Client/Data/", [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    if ($relative -eq "Assets/profiles-background.png" -or $relative -eq "Assets/settings-background.png") {
        continue
    }

    $local = Join-Path $InstallRoot ($relative -replace '/', [System.IO.Path]::DirectorySeparatorChar)
    if (-not (Test-Path $local)) {
        $outdated += "$relative (missing)"
        continue
    }

    $info = Get-Item $local
    if ($info.Length -ne [int64]$entry.size) {
        $outdated += "$relative (size local=$($info.Length) expected=$($entry.size))"
        continue
    }

    $hash = (Get-FileHash $local -Algorithm SHA256).Hash.ToLowerInvariant()
    if ($hash -ne [string]$entry.sha256) {
        $outdated += "$relative (hash)"
    }
}

if ($outdated.Count -gt 0) {
    Write-Host "Install verification FAILED ($($outdated.Count) file(s) out of date with manifest):"
    $outdated | ForEach-Object { Write-Host "  $_" }
    exit 1
}

Write-Host "Install verification passed ($($manifest.files.Count) manifest entries checked)."
exit 0
