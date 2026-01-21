# ================================================================================
# Tarklog Build Script (PowerShell)
# ================================================================================
# This script builds the Tarklog application for distribution
#
# Requirements:
#   - .NET 8.0 SDK installed
#   - Run from the project root directory
#
# Build Options:
#   1. Framework-dependent (requires .NET 8.0 Runtime on target machine)
#   2. Self-contained (includes .NET Runtime, larger file size)
# ================================================================================

# Set error action preference
$ErrorActionPreference = "Stop"

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Tarklog Build Script" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

# Check if .NET SDK is available
try {
    $dotnetVersion = dotnet --version
    Write-Host ".NET SDK Version: $dotnetVersion" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR: .NET SDK not found. Please install .NET 8.0 SDK." -ForegroundColor Red
    Write-Host "Download from: https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor Yellow
    Read-Host "Press Enter to exit"
    exit 1
}

# Change to the project directory
$projectDir = Join-Path $PSScriptRoot "Tarklog"
Set-Location $projectDir

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Step 1: Cleaning previous builds..." -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan

try {
    dotnet clean --configuration Release | Out-Null
    Write-Host "Clean completed successfully." -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "ERROR: Clean failed" -ForegroundColor Red
    Read-Host "Press Enter to exit"
    exit 1
}

Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Step 2: Choose Build Type" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Select build type:" -ForegroundColor Yellow
Write-Host "  1. Framework-dependent (smaller, requires .NET 8.0 Runtime)" -ForegroundColor White
Write-Host "  2. Self-contained (larger, includes .NET Runtime)" -ForegroundColor White
Write-Host "  3. Build both versions" -ForegroundColor White
Write-Host ""

$choice = Read-Host "Enter your choice (1, 2, or 3)"

function Build-FrameworkDependent {
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "Building Framework-Dependent Version..." -ForegroundColor Cyan
    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "Output: dist\framework-dependent\" -ForegroundColor White
    Write-Host "Size: ~2.9 MB" -ForegroundColor White
    Write-Host "Requires: .NET 8.0 Desktop Runtime on target machine" -ForegroundColor White
    Write-Host ""

    try {
        $distPath = Join-Path (Split-Path $projectDir -Parent) "dist\framework-dependent"
        dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $distPath

        Write-Host ""
        Write-Host "Framework-dependent build completed successfully!" -ForegroundColor Green
        Write-Host "Location: $distPath" -ForegroundColor White

        # Get file size
        $exePath = Join-Path $distPath "Tarklog.exe"
        if (Test-Path $exePath) {
            $fileSize = (Get-Item $exePath).Length / 1MB
            Write-Host "Tarklog.exe size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor White
        }
        Write-Host ""

        return $true
    } catch {
        Write-Host "ERROR: Framework-dependent build failed" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

function Build-SelfContained {
    Write-Host ""
    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "Building Self-Contained Version..." -ForegroundColor Cyan
    Write-Host "================================================================================" -ForegroundColor Cyan
    Write-Host "Output: dist\self-contained\" -ForegroundColor White
    Write-Host "Size: ~70-80 MB" -ForegroundColor White
    Write-Host "Requires: No additional runtime installation needed" -ForegroundColor White
    Write-Host ""

    try {
        $distPath = Join-Path (Split-Path $projectDir -Parent) "dist\self-contained"
        dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o $distPath

        Write-Host ""
        Write-Host "Self-contained build completed successfully!" -ForegroundColor Green
        Write-Host "Location: $distPath" -ForegroundColor White

        # Get file size
        $exePath = Join-Path $distPath "Tarklog.exe"
        if (Test-Path $exePath) {
            $fileSize = (Get-Item $exePath).Length / 1MB
            Write-Host "Tarklog.exe size: $([math]::Round($fileSize, 2)) MB" -ForegroundColor White
        }
        Write-Host ""

        return $true
    } catch {
        Write-Host "ERROR: Self-contained build failed" -ForegroundColor Red
        Write-Host $_.Exception.Message -ForegroundColor Red
        return $false
    }
}

# Execute builds based on choice
$frameworkSuccess = $false
$selfContainedSuccess = $false

switch ($choice) {
    "1" {
        $frameworkSuccess = Build-FrameworkDependent
    }
    "2" {
        $selfContainedSuccess = Build-SelfContained
    }
    "3" {
        $frameworkSuccess = Build-FrameworkDependent
        $selfContainedSuccess = Build-SelfContained
    }
    default {
        Write-Host "Invalid choice. Exiting." -ForegroundColor Red
        Read-Host "Press Enter to exit"
        exit 1
    }
}

# Build Summary
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Build Summary" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host ""

$rootDir = Split-Path $projectDir -Parent

if ($choice -eq "1" -and $frameworkSuccess) {
    Write-Host "Framework-dependent build: " -NoNewline -ForegroundColor White
    Write-Host "COMPLETED" -ForegroundColor Green
    Write-Host "  Location: $(Join-Path $rootDir 'dist\framework-dependent\')" -ForegroundColor White
    Write-Host "  Main executable: Tarklog.exe (~2.9 MB)" -ForegroundColor White
    Write-Host "  Requires: .NET 8.0 Desktop Runtime" -ForegroundColor Yellow
}

if ($choice -eq "2" -and $selfContainedSuccess) {
    Write-Host "Self-contained build: " -NoNewline -ForegroundColor White
    Write-Host "COMPLETED" -ForegroundColor Green
    Write-Host "  Location: $(Join-Path $rootDir 'dist\self-contained\')" -ForegroundColor White
    Write-Host "  Main executable: Tarklog.exe (~70-80 MB)" -ForegroundColor White
    Write-Host "  Requires: No additional runtime" -ForegroundColor Green
}

if ($choice -eq "3") {
    if ($frameworkSuccess) {
        Write-Host "Framework-dependent build: " -NoNewline -ForegroundColor White
        Write-Host "COMPLETED" -ForegroundColor Green
        Write-Host "  Location: $(Join-Path $rootDir 'dist\framework-dependent\')" -ForegroundColor White
        Write-Host "  Main executable: Tarklog.exe (~2.9 MB)" -ForegroundColor White
    } else {
        Write-Host "Framework-dependent build: " -NoNewline -ForegroundColor White
        Write-Host "FAILED" -ForegroundColor Red
    }

    Write-Host ""

    if ($selfContainedSuccess) {
        Write-Host "Self-contained build: " -NoNewline -ForegroundColor White
        Write-Host "COMPLETED" -ForegroundColor Green
        Write-Host "  Location: $(Join-Path $rootDir 'dist\self-contained\')" -ForegroundColor White
        Write-Host "  Main executable: Tarklog.exe (~70-80 MB)" -ForegroundColor White
    } else {
        Write-Host "Self-contained build: " -NoNewline -ForegroundColor White
        Write-Host "FAILED" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "Next Steps:" -ForegroundColor Cyan
Write-Host "================================================================================" -ForegroundColor Cyan
Write-Host "1. Test the executable(s) on a clean Windows machine" -ForegroundColor White
Write-Host "2. Create a ZIP file of the publish folder for distribution" -ForegroundColor White
Write-Host "3. Optionally create an installer using Inno Setup or WiX Toolset" -ForegroundColor White
Write-Host ""
Write-Host "For framework-dependent builds, users need:" -ForegroundColor Yellow
Write-Host "  .NET 8.0 Desktop Runtime from https://dotnet.microsoft.com/download/dotnet/8.0" -ForegroundColor White
Write-Host ""
Write-Host "================================================================================" -ForegroundColor Cyan

Read-Host "Press Enter to exit"
