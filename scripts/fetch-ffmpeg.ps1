$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Tools = Join-Path $Root "tools"
$Zip = Join-Path $Tools "ffmpeg-release-essentials.zip"
$Extract = Join-Path $Tools "ffmpeg-extract"
$Bin = Join-Path $Root "tools\ffmpeg\bin"
$Url = "https://www.gyan.dev/ffmpeg/builds/ffmpeg-release-essentials.zip"

function Assert-UnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RootPath
    )

    $full = [System.IO.Path]::GetFullPath($Path)
    $rootFull = [System.IO.Path]::GetFullPath($RootPath)
    if (-not $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside project tools folder: $full"
    }
}

New-Item -ItemType Directory -Force -Path $Tools | Out-Null
Assert-UnderRoot -Path $Extract -RootPath $Tools
Assert-UnderRoot -Path $Bin -RootPath $Tools

if (Test-Path -LiteralPath $Extract) {
    Remove-Item -LiteralPath $Extract -Recurse -Force
}

Write-Host "Downloading FFmpeg..."
curl.exe -L $Url -o $Zip

Write-Host "Extracting FFmpeg..."
New-Item -ItemType Directory -Force -Path $Extract | Out-Null
Expand-Archive -LiteralPath $Zip -DestinationPath $Extract -Force

$Ffmpeg = Get-ChildItem -LiteralPath $Extract -Recurse -Filter "ffmpeg.exe" | Select-Object -First 1
if (-not $Ffmpeg) {
    throw "ffmpeg.exe was not found in the downloaded archive."
}

New-Item -ItemType Directory -Force -Path $Bin | Out-Null
Copy-Item -LiteralPath $Ffmpeg.FullName -Destination (Join-Path $Bin "ffmpeg.exe") -Force

$Ffprobe = Get-ChildItem -LiteralPath $Extract -Recurse -Filter "ffprobe.exe" | Select-Object -First 1
if ($Ffprobe) {
    Copy-Item -LiteralPath $Ffprobe.FullName -Destination (Join-Path $Bin "ffprobe.exe") -Force
}

Remove-Item -LiteralPath $Extract -Recurse -Force
Write-Host "FFmpeg is ready at $(Join-Path $Bin 'ffmpeg.exe')"

