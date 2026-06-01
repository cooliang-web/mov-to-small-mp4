$ErrorActionPreference = "Stop"

$Root = Split-Path -Parent $PSScriptRoot
$Source = Join-Path $Root "src\MovToSmallMp4.cs"
$Dist = Join-Path $Root "dist"
$Out = Join-Path $Dist "MovToSmallMp4.exe"
$Framework = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319"
$Wpf = Join-Path $Framework "WPF"

New-Item -ItemType Directory -Force -Path $Dist | Out-Null

$References = @(
    (Join-Path $Framework "System.dll"),
    (Join-Path $Framework "System.Core.dll"),
    (Join-Path $Framework "System.Drawing.dll"),
    (Join-Path $Framework "System.Windows.Forms.dll"),
    (Join-Path $Wpf "WindowsBase.dll"),
    (Join-Path $Wpf "PresentationCore.dll"),
    (Join-Path $Wpf "PresentationFramework.dll"),
    (Join-Path $Wpf "WindowsFormsIntegration.dll"),
    (Join-Path $Framework "System.Xaml.dll")
)

Add-Type `
    -TypeDefinition (Get-Content -LiteralPath $Source -Raw -Encoding UTF8) `
    -ReferencedAssemblies $References `
    -OutputAssembly $Out `
    -OutputType WindowsApplication `
    -Language CSharp

Write-Host "Built $Out"
