@echo off
REM ================================================================================
REM Tarklog Build Script
REM ================================================================================
REM This script builds the Tarklog application for distribution
REM
REM Requirements:
REM   - .NET 8.0 SDK installed
REM   - Run from the project root directory
REM
REM Build Options:
REM   1. Framework-dependent (requires .NET 8.0 Runtime on target machine)
REM   2. Self-contained (includes .NET Runtime, larger file size)
REM ================================================================================

echo.
echo ================================================================================
echo Tarklog Build Script
echo ================================================================================
echo.

REM Check if .NET SDK is available
dotnet --version >nul 2>&1
if %errorlevel% neq 0 (
    echo ERROR: .NET SDK not found. Please install .NET 8.0 SDK.
    echo Download from: https://dotnet.microsoft.com/download/dotnet/8.0
    pause
    exit /b 1
)

echo .NET SDK Version:
dotnet --version
echo.

REM Change to the project directory
cd /d "%~dp0\Tarklog"

echo ================================================================================
echo Step 1: Cleaning previous builds...
echo ================================================================================
dotnet clean --configuration Release
if %errorlevel% neq 0 (
    echo ERROR: Clean failed
    pause
    exit /b 1
)
echo Clean completed successfully.
echo.

echo ================================================================================
echo Step 2: Choose Build Type
echo ================================================================================
echo.
echo Select build type:
echo   1. Framework-dependent (smaller, requires .NET 8.0 Runtime)
echo   2. Self-contained (larger, includes .NET Runtime)
echo   3. Build both versions
echo.
choice /c 123 /n /m "Enter your choice (1, 2, or 3): "
set BUILD_CHOICE=%errorlevel%

echo.

REM Framework-dependent build
if %BUILD_CHOICE%==1 goto BUILD_FRAMEWORK
if %BUILD_CHOICE%==3 goto BUILD_FRAMEWORK
goto BUILD_SELFCONTAINED

:BUILD_FRAMEWORK
echo ================================================================================
echo Building Framework-Dependent Version...
echo ================================================================================
echo Output: dist\framework-dependent\
echo Size: ~2.9 MB
echo Requires: .NET 8.0 Desktop Runtime on target machine
echo.

dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\dist\framework-dependent
if %errorlevel% neq 0 (
    echo ERROR: Framework-dependent build failed
    pause
    exit /b 1
)

echo.
echo Framework-dependent build completed successfully!
echo Location: %CD%\..\dist\framework-dependent\
echo.

if %BUILD_CHOICE%==1 goto BUILD_COMPLETE

:BUILD_SELFCONTAINED
echo ================================================================================
echo Building Self-Contained Version...
echo ================================================================================
echo Output: dist\self-contained\
echo Size: ~70-80 MB
echo Requires: No additional runtime installation needed
echo.

dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\dist\self-contained
if %errorlevel% neq 0 (
    echo ERROR: Self-contained build failed
    pause
    exit /b 1
)

echo.
echo Self-contained build completed successfully!
echo Location: %CD%\..\dist\self-contained\
echo.

:BUILD_COMPLETE
echo ================================================================================
echo Build Summary
echo ================================================================================

if %BUILD_CHOICE%==1 (
    echo Framework-dependent build: COMPLETED
    echo   Location: %CD%\..\dist\framework-dependent\
    echo   Main executable: Tarklog.exe (~2.9 MB)
    echo   Requires: .NET 8.0 Desktop Runtime
)

if %BUILD_CHOICE%==2 (
    echo Self-contained build: COMPLETED
    echo   Location: %CD%\..\dist\self-contained\
    echo   Main executable: Tarklog.exe (~70-80 MB)
    echo   Requires: No additional runtime
)

if %BUILD_CHOICE%==3 (
    echo Framework-dependent build: COMPLETED
    echo   Location: %CD%\..\dist\framework-dependent\
    echo   Main executable: Tarklog.exe (~2.9 MB)
    echo.
    echo Self-contained build: COMPLETED
    echo   Location: %CD%\..\dist\self-contained\
    echo   Main executable: Tarklog.exe (~70-80 MB)
)

echo.
echo ================================================================================
echo Next Steps:
echo ================================================================================
echo 1. Test the executable(s) on a clean Windows machine
echo 2. Create a ZIP file of the publish folder for distribution
echo 3. Optionally create an installer using Inno Setup or WiX Toolset
echo.
echo For framework-dependent builds, users need:
echo   .NET 8.0 Desktop Runtime from https://dotnet.microsoft.com/download/dotnet/8.0
echo.
echo ================================================================================

pause
