param(
    [Parameter(Mandatory = $true)]
    [string]$OldManifestPath,

    [Parameter(Mandatory = $true)]
    [string]$NewManifestPath
)

$ErrorActionPreference = "Stop"

if (-not (Test-Path $OldManifestPath)) {
    Write-Error "Old manifest not found: $OldManifestPath"
}
if (-not (Test-Path $NewManifestPath)) {
    Write-Error "New manifest not found: $NewManifestPath"
}

$oldManifest = Get-Content $OldManifestPath -Raw | ConvertFrom-Json
$newManifest = Get-Content $NewManifestPath -Raw | ConvertFrom-Json

Write-Host "Old manifest version: $($oldManifest.version)"
Write-Host "New manifest version: $($newManifest.version)"
Write-Host ""

$oldByPath = @{}
foreach ($entry in $oldManifest.files) {
    if (-not [string]::IsNullOrWhiteSpace([string]$entry.path)) {
        $oldByPath[[string]$entry.path] = [string]$entry.sha256
    }
}

$wouldUpdate = @()
foreach ($entry in $newManifest.files) {
    $path = [string]$entry.path
    if ([string]::IsNullOrWhiteSpace($path)) {
        continue
    }

    if ($path.StartsWith("ClassicUO/", [System.StringComparison]::OrdinalIgnoreCase) -or
        $path.StartsWith("Client/Data/", [System.StringComparison]::OrdinalIgnoreCase)) {
        continue
    }

    if ($path -eq "Assets/profiles-background.png" -or $path -eq "Assets/settings-background.png") {
        continue
    }

    $newHash = [string]$entry.sha256
    if ($oldByPath.ContainsKey($path)) {
        if ($oldByPath[$path] -ne $newHash) {
            $wouldUpdate += "$path (hash changed)"
        }
    }
    else {
        $wouldUpdate += "$path (new in manifest)"
    }
}

if ($wouldUpdate.Count -eq 0) {
    Write-Error "No manifest differences detected between v$($oldManifest.version) and v$($newManifest.version)."
}

Write-Host "Update detection OK: a v$($oldManifest.version) install would update $($wouldUpdate.Count) file(s) to v$($newManifest.version):"
$wouldUpdate | ForEach-Object { Write-Host "  $_" }
