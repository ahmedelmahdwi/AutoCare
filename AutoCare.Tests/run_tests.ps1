$ErrorActionPreference = "Stop"

$baseDir = "C:\Users\UEFI\Desktop\AutoCare_prjoct\FFF\C# project_Auto_Care COMPLETE"
$wpfDir = Join-Path $baseDir "WPF_AutoCare"
$testDir = Join-Path $baseDir "AutoCare.Tests"
$temp = "C:\Users\UEFI\AppData\Local\Temp\opencode"
$csc = "$temp\compilers-toolset-4.8.0\tasks\net472\csc.exe"
$refs = Join-Path ${env:ProgramFiles(x86)} "Reference Assemblies\Microsoft\Framework\.NETFramework\v4.8.1"

$bin = Join-Path $testDir "bin"
New-Item -ItemType Directory -Force -Path $bin | Out-Null
$testExe = Join-Path $bin "AutoCare.Tests.exe"

Write-Host "== Compiling AutoCare Automated Test Suite =="

$refArgs = @()
$needRefs = @(
    "System.dll", "System.Core.dll", "System.Data.dll", "System.Drawing.dll",
    "System.Xml.dll", "System.Configuration.dll", "System.Security.dll",
    "WindowsBase.dll", "PresentationCore.dll", "PresentationFramework.dll",
    "System.IO.Compression.dll", "System.IO.Compression.FileSystem.dll"
)
foreach ($r in $needRefs) {
    $p = Join-Path $refs $r
    if (Test-Path $p) { $refArgs += "/reference:$p" }
}

$appExe = Join-Path $wpfDir "bin\Release\WPF Auto Care.exe"
if (-not (Test-Path $appExe)) {
    Write-Host "Application executable not found, building first..."
    & powershell -ExecutionPolicy Bypass -File (Join-Path $baseDir "build_clean_solution.ps1")
}

$domainDll = Join-Path $baseDir "AutoCare.Domain\bin\Release\AutoCare.Domain.dll"
$appDll = Join-Path $baseDir "AutoCare.Application\bin\Release\AutoCare.Application.dll"
$infraDll = Join-Path $baseDir "AutoCare.Infrastructure\bin\Release\AutoCare.Infrastructure.dll"

if (Test-Path $domainDll) { $refArgs += "/reference:$domainDll" }
if (Test-Path $appDll) { $refArgs += "/reference:$appDll" }
if (Test-Path $infraDll) { $refArgs += "/reference:$infraDll" }

$refArgs += "/reference:$appExe"

& $csc /nologo /target:exe /langversion:latest /platform:anycpu /warn:0 /codepage:65001 /utf8output `
    "/out:$testExe" `
    $refArgs `
    (Join-Path $testDir "TestRunner.cs")

if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE"
}

Write-Host "== Staging Test Binaries =="
Copy-Item $appExe $bin -Force
if (Test-Path $domainDll) { Copy-Item $domainDll $bin -Force }
if (Test-Path $appDll) { Copy-Item $appDll $bin -Force }
if (Test-Path $infraDll) { Copy-Item $infraDll $bin -Force }
$appConfig = "$appExe.config"
if (Test-Path $appConfig) {
    Copy-Item $appConfig $bin -Force
}
if (Test-Path "$wpfDir\Views") {
    Copy-Item "$wpfDir\Views" $bin -Recurse -Force
}
if (Test-Path "$wpfDir\Theme\Colors.xaml") {
    Copy-Item "$wpfDir\Theme\Colors.xaml" "$bin\Colors.xaml" -Force
}

Write-Host "== Running Test Suite =="
Write-Host ""
Push-Location $bin
try {
    & $testExe
    $testExitCode = $LASTEXITCODE
} finally {
    Pop-Location
}

if ($testExitCode -ne 0) {
    throw "Tests failed with exit code $testExitCode"
}

Write-Host "`nTest Suite Completed with Exit Code: $testExitCode"
