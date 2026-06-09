# Exports button layers from Profiles/Settings PXZ only. Does NOT touch background PNGs.
$ErrorActionPreference = "Stop"
Add-Type -AssemblyName PresentationCore

function Test-PngHasAlpha([string]$path) {
    $img = New-Object System.Windows.Media.Imaging.BitmapImage
    $img.BeginInit()
    $img.UriSource = [Uri]((Resolve-Path $path).Path)
    $img.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $img.EndInit()
    $hasAlpha = $img.Format -match "Pbgra|Bgra|Alpha"
    return [PSCustomObject]@{ Path = $path; Width = $img.PixelWidth; Height = $img.PixelHeight; Format = $img.Format; HasAlpha = $hasAlpha }
}

function Export-Png([string]$src, [string]$dest) {
    Copy-Item $src $dest -Force
    return Test-PngHasAlpha $dest
}

function Export-Webp([string]$src, [string]$dest) {
    $uri = [Uri]((Resolve-Path $src).Path)
    $img = New-Object System.Windows.Media.Imaging.BitmapImage
    $img.BeginInit()
    $img.UriSource = $uri
    $img.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $img.EndInit()
    $img.Freeze()
    $converted = New-Object System.Windows.Media.Imaging.FormatConvertedBitmap(
        $img,
        [System.Windows.Media.PixelFormats]::Pbgra32,
        $null,
        0)
    $enc = New-Object System.Windows.Media.Imaging.PngBitmapEncoder
    $enc.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($converted))
    $fs = [IO.File]::OpenWrite($dest)
    try { $enc.Save($fs) } finally { $fs.Close() }
    return Test-PngHasAlpha $dest
}

$pxzRoot = "c:\Users\SNTre\OneDrive\Documents\ClassicUONewAssets"
$stage = Join-Path $env:TEMP "pxz-button-export"
Remove-Item $stage -Recurse -Force -ErrorAction SilentlyContinue
Add-Type -AssemblyName System.IO.Compression.FileSystem

$profilesOut = "D:\SkythornLauncher\Assets\Menu\Profiles"
$settingsOut = "D:\SkythornLauncher\Assets\Menu\Settings"
New-Item -ItemType Directory -Force -Path $profilesOut, $settingsOut | Out-Null

foreach ($name in @("Profiles Menu.pxz", "Settings Menu.pxz")) {
    $extract = Join-Path $stage ($name -replace '\.pxz$','')
    [System.IO.Compression.ZipFile]::ExtractToDirectory((Join-Path $pxzRoot $name), $extract)
}

$profiles = Join-Path $stage "Profiles Menu"
$settings = Join-Path $stage "Settings Menu"

$map = @(
    @{ Src = Join-Path $profiles "dc7a99554169.png"; Dest = Join-Path $profilesOut "new.png" },
    @{ Src = Join-Path $profiles "fe69bf284e5d.png"; Dest = Join-Path $profilesOut "new-hover.png" },
    @{ Src = Join-Path $profiles "41bc96f240d2.png"; Dest = Join-Path $profilesOut "delete.png" },
    @{ Src = Join-Path $profiles "e298b2284855.png"; Dest = Join-Path $profilesOut "delete-hover.png" },
    @{ Src = Join-Path $profiles "2e01ae404528.png"; Dest = Join-Path $profilesOut "cancel.png" },
    @{ Src = Join-Path $profiles "03dba6b044ef.png"; Dest = Join-Path $profilesOut "cancel-hover.png" },
    @{ Src = Join-Path $profiles "f77f94d24622.png"; Dest = Join-Path $profilesOut "save.png" },
    @{ Src = Join-Path $profiles "75ffaae6453b.png"; Dest = Join-Path $profilesOut "save-hover.png" },
    @{ Src = Join-Path $settings "40f596ef44c0.webp"; Dest = Join-Path $settingsOut "cancel.png"; Webp = $true },
    @{ Src = Join-Path $settings "9510bdfa4ac7.webp"; Dest = Join-Path $settingsOut "cancel-hover.png"; Webp = $true },
    @{ Src = Join-Path $settings "dafeaf334d65.webp"; Dest = Join-Path $settingsOut "save.png"; Webp = $true },
    @{ Src = Join-Path $settings "fd65b89e4be2.webp"; Dest = Join-Path $settingsOut "save-hover.png"; Webp = $true }
)

Write-Host "Exporting button layers (backgrounds untouched)..."
foreach ($item in $map) {
    $info = if ($item.Webp) { Export-Webp $item.Src $item.Dest } else { Export-Png $item.Src $item.Dest }
    $alpha = if ($info.HasAlpha) { "alpha OK" } else { "CHECK ALPHA" }
    Write-Host ("  {0} -> {1} ({2}x{3}, {4})" -f (Split-Path $item.Src -Leaf), (Split-Path $item.Dest -Leaf), $info.Width, $info.Height, $alpha)
}

Write-Host "Done."

# Backgrounds: copy from OneDrive flat exports (Profiles Menu.png / Settings Menu.png).
# Do not use the PXZ Background layer unless those PNGs are updated too.
