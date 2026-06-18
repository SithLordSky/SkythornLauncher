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

Write-Host "Published SkythornLauncher.exe to $root (player build only)"
