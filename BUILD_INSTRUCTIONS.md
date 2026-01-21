# Tarklog Build Instructions

This document explains how to build Tarklog for distribution.

## Prerequisites

- .NET 8.0 SDK installed
- Windows operating system
- Git (optional, for version control)

Download .NET 8.0 SDK from: https://dotnet.microsoft.com/download/dotnet/8.0

## Build Scripts

Two build scripts are provided in the project root:

### 1. build.bat (Batch Script)
**Recommended for:** Windows Command Prompt users

```batch
build.bat
```

### 2. build.ps1 (PowerShell Script)
**Recommended for:** PowerShell users (better formatting and color output)

```powershell
.\build.ps1
```

**Note:** If you get an execution policy error, run:
```powershell
Set-ExecutionPolicy -ExecutionPolicy RemoteSigned -Scope CurrentUser
```

## Build Options

Both scripts offer three build options:

### Option 1: Framework-Dependent Build
- **Size:** ~2.9 MB
- **Pros:** Smaller file size, faster download
- **Cons:** Requires .NET 8.0 Desktop Runtime on target machine
- **Best for:** Users who already have .NET installed
- **Output:** `dist\framework-dependent\`

### Option 2: Self-Contained Build
- **Size:** ~70-80 MB
- **Pros:** No runtime installation needed, works on any Windows machine
- **Cons:** Larger file size
- **Best for:** Users who don't have .NET installed
- **Output:** `dist\self-contained\`

### Option 3: Build Both Versions
- Builds both framework-dependent and self-contained versions
- Useful for offering users a choice

## Manual Build Commands

If you prefer to build manually:

### Framework-Dependent:
```bash
cd Tarklog
dotnet clean -c Release
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\dist\framework-dependent
```

### Self-Contained:
```bash
cd Tarklog
dotnet clean -c Release
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o ..\dist\self-contained
```

## Distribution

After building:

1. Navigate to the appropriate publish folder
2. Test `Tarklog.exe` to ensure it works
3. Create a ZIP file containing:
   - `Tarklog.exe`
   - `tooltip_where_log.png`
   - `README.txt` (user documentation)
   - Localization folders (optional)

### Creating a ZIP File (PowerShell):
```powershell
# Framework-dependent
Compress-Archive -Path "Tarklog\bin\Release\net8.0-windows\win-x64\publish\*" -DestinationPath "Tarklog-v1.0-FrameworkDependent.zip"

# Self-contained
Compress-Archive -Path "Tarklog\bin\Release\net8.0-windows\win-x64-standalone\publish\*" -DestinationPath "Tarklog-v1.0-Standalone.zip"
```

## Troubleshooting

### Build fails with "project is currently running"
- Close any running instances of Tarklog
- Try again

### .NET SDK not found
- Install .NET 8.0 SDK from the link above
- Restart your terminal/command prompt
- Verify with: `dotnet --version`

### Permission denied errors
- Run the build script as Administrator
- Check that antivirus isn't blocking the build

### Build warnings (CS8603, CS8604, etc.)
- These are nullability warnings and are expected
- The application will still build and run correctly

## Project Structure

```
TARKLOG/
├── build.bat                 # Batch build script
├── build.ps1                 # PowerShell build script
├── BUILD_INSTRUCTIONS.md     # This file
├── IMPLEMENTATION_PLAN.md    # Project implementation details
└── Tarklog/                  # Main project folder
    ├── Tarklog.csproj        # Project file
    ├── MainWindow.xaml       # Main UI
    ├── MainWindow.xaml.cs    # Main logic
    ├── Services/             # Business logic
    ├── Database/             # Database management
    ├── Models/               # Data models
    └── Data/                 # Static data (country codes, etc.)
```

## Advanced: Creating an Installer

For a professional installer, consider:

### Inno Setup (Free, Recommended)
1. Download from: https://jrsoftware.org/isinfo.php
2. Create a script that:
   - Installs the application to Program Files
   - Creates Start Menu shortcuts
   - Optionally checks for .NET Runtime (framework-dependent builds)

### WiX Toolset (Free, Advanced)
1. Download from: https://wixtoolset.org/
2. Create MSI installers with full Windows Installer features

### Advanced Installer (Commercial)
1. Download from: https://www.advancedinstaller.com/
2. GUI-based installer creation

## Version Management

To update the version number:

1. Edit `Tarklog\Tarklog.csproj`
2. Add version properties:
```xml
<PropertyGroup>
  <Version>1.0.0</Version>
  <AssemblyVersion>1.0.0.0</AssemblyVersion>
  <FileVersion>1.0.0.0</FileVersion>
</PropertyGroup>
```

## Support

For issues or questions:
- Check the troubleshooting section above
- Review build output for specific error messages
- Ensure all prerequisites are met
