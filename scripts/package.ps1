$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Dist = Join-Path $Root "dist"
$Exe = Join-Path $Dist "MovToSmallMp4.exe"
$Ffmpeg = Join-Path $Root "tools\ffmpeg\bin\ffmpeg.exe"
$PackageDir = Join-Path $Dist "MovToSmallMp4-windows"
$Zip = Join-Path $Dist "MovToSmallMp4-windows.zip"

function Assert-UnderRoot {
    param(
        [Parameter(Mandatory = $true)][string]$Path,
        [Parameter(Mandatory = $true)][string]$RootPath
    )

    $full = [System.IO.Path]::GetFullPath($Path)
    $rootFull = [System.IO.Path]::GetFullPath($RootPath)
    if (-not $full.StartsWith($rootFull, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside dist folder: $full"
    }
}

if (-not (Test-Path -LiteralPath $Exe)) {
    & (Join-Path $PSScriptRoot "build-windows-exe.ps1")
}

New-Item -ItemType Directory -Force -Path $Dist | Out-Null
Assert-UnderRoot -Path $PackageDir -RootPath $Dist
Assert-UnderRoot -Path $Zip -RootPath $Dist

if (Test-Path -LiteralPath $PackageDir) {
    Remove-Item -LiteralPath $PackageDir -Recurse -Force
}
if (Test-Path -LiteralPath $Zip) {
    Remove-Item -LiteralPath $Zip -Force
}

New-Item -ItemType Directory -Force -Path $PackageDir | Out-Null
Copy-Item -LiteralPath $Exe -Destination (Join-Path $PackageDir "MovToSmallMp4.exe") -Force
Copy-Item -LiteralPath (Join-Path $Root "README.md") -Destination (Join-Path $PackageDir "README.md") -Force
Copy-Item -LiteralPath (Join-Path $Root "LICENSE") -Destination (Join-Path $PackageDir "LICENSE") -Force
Copy-Item -LiteralPath (Join-Path $Root "THIRD_PARTY_NOTICES.md") -Destination (Join-Path $PackageDir "THIRD_PARTY_NOTICES.md") -Force

if (Test-Path -LiteralPath $Ffmpeg) {
    Copy-Item -LiteralPath $Ffmpeg -Destination (Join-Path $PackageDir "ffmpeg.exe") -Force
}
else {
    Write-Warning "ffmpeg.exe was not found. The package will require ffmpeg.exe next to the app."
}

Compress-Archive -Path (Join-Path $PackageDir "*") -DestinationPath $Zip -Force
Write-Host "Created $Zip"
