# Publishes to a staging folder, then copies runtime files to the install root.
$ErrorActionPreference = "Stop"
$root = $PSScriptRoot
$stage = Join-Path $root ".publish"

Stop-Process -Name SkythornLauncher,ShardStatusServer -ErrorAction SilentlyContinue
Start-Sleep -Milliseconds 500

if (Test-Path $stage) {
    Remove-Item $stage -Recurse -Force
}

dotnet publish (Join-Path $root "SkythornLauncher.csproj") -c Release -r win-x64 -o $stage
dotnet publish (Join-Path $root "ShardStatusServer\ShardStatusServer.csproj") -c Release -r win-x64 -o (Join-Path $stage "StatusServer")

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

$statusDest = Join-Path $root "StatusServer"
if (Test-Path $statusDest) {
    Remove-Item $statusDest -Recurse -Force
}
Copy-Item (Join-Path $stage "StatusServer") $statusDest -Recurse -Force

Remove-Item $stage -Recurse -Force

# Clean legacy / build clutter (keep source, Assets, ClassicUO, Client, StatusServer runtime)
$cleanPaths = @(
    (Join-Path $root "bin"),
    (Join-Path $root ".publish"),
    (Join-Path $root "ShardStatusServer\bin"),
    (Join-Path $root "ShardStatusServer\obj")
)
foreach ($path in $cleanPaths) {
    if (Test-Path $path) {
        Remove-Item $path -Recurse -Force
    }
}

Get-ChildItem $root -Filter "*.pdb" -File -ErrorAction SilentlyContinue | Remove-Item -Force
Get-ChildItem (Join-Path $root "StatusServer") -Filter "*.pdb" -File -ErrorAction SilentlyContinue | Remove-Item -Force

Write-Host "Published SkythornLauncher.exe to $root (cleaned legacy bin and build artifacts)"
