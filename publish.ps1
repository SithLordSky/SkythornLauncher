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
Copy-Item (Join-Path $stage "Assets") $root -Recurse -Force

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
