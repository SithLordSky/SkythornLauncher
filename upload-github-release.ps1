param(
    [string]$Repo = "SithLordSky/SkythornLauncher",
    [string]$Tag = "v0.9.9",
    [string]$Title = "v0.9.9",
    [string]$Root = $PSScriptRoot
)

$ErrorActionPreference = "Stop"

$token = $env:GITHUB_TOKEN
if ([string]::IsNullOrWhiteSpace($token)) {
    throw "GITHUB_TOKEN is required to create a GitHub Release."
}

$assetsDir = Join-Path $Root ".release\assets"
$manifestPath = Join-Path $Root ".release\update-manifest.json"
if (-not (Test-Path $assetsDir)) {
    throw "Release assets folder not found: $assetsDir"
}
if (-not (Test-Path $manifestPath)) {
    throw "Release manifest not found: $manifestPath"
}

$manifest = Get-Content $manifestPath -Raw | ConvertFrom-Json
$releaseNotes = if ($manifest.releaseNotes) { [string]$manifest.releaseNotes } else { "Skythorn Launcher $Tag" }

$headers = @{
    Authorization = "Bearer $token"
    Accept        = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

$releaseBody = @{
    tag_name         = $Tag
    name             = $Title
    body             = $releaseNotes
    draft            = $false
    prerelease       = $false
    make_latest      = "true"
    generate_release_notes = $false
} | ConvertTo-Json

Write-Host "Creating release $Tag..."
$release = Invoke-RestMethod `
    -Method Post `
    -Uri "https://api.github.com/repos/$Repo/releases" `
    -Headers $headers `
    -Body $releaseBody `
    -ContentType "application/json; charset=utf-8"

$uploadHeaders = @{
    Authorization = "Bearer $token"
    Accept        = "application/vnd.github+json"
    "X-GitHub-Api-Version" = "2022-11-28"
}

$files = Get-ChildItem $assetsDir -File | Sort-Object Name
foreach ($file in $files) {
    $encodedName = [Uri]::EscapeDataString($file.Name)
    $uploadUrl = "https://uploads.github.com/repos/$Repo/releases/$($release.id)/assets?name=$encodedName"
    Write-Host "Uploading $($file.Name)..."
    Invoke-RestMethod `
        -Method Post `
        -Uri $uploadUrl `
        -Headers $uploadHeaders `
        -ContentType "application/octet-stream" `
        -InFile $file.FullName | Out-Null
}

Write-Host "Release published: $($release.html_url)"
