# Tarklog - Log File Manager

A Windows desktop application for scanning, parsing, and analyzing Escape from Tarkov game log files across multiple directories.

## Overview

Tarklog is a comprehensive log management tool designed to help players and administrators organize and analyze game logs from Escape from Tarkov. It automatically discovers log files scattered across your system, parses them to extract meaningful data, and stores everything in a local SQLite database for easy querying and analysis.

## Features

### 📁 Directory Management
- **Dual Directory Support**: Scan both a main "Log Root" directory and a backup "Log Storage" directory
- **Recursive Scanning**: Automatically discovers all subdirectories and log files
- **Flexible File Discovery**: Supports both current (`application_000.log`) and legacy (`application.log`) file naming conventions
- **Real-time Progress**: Two-phase progress dialog showing file discovery and parsing stages

### 📊 Data Parsing & Storage
- **Automatic Log Parsing**: Extracts structured data from unformatted log files
- **SQLite Database**: All scanned files and parsed data are stored persistently
- **Data Extraction**: Captures timestamps, IP addresses, map locations, raid IDs, and data center information
- **Duplicate Prevention**: Avoids re-processing already scanned files

### 📈 Analytics & Visualization
- **Map Distribution**: Pie chart showing which maps appear most frequently in your logs
- **Timeline View**: Recent log entries displayed in chronological order with filtering options
- **Summary Panel**: Quick view of the most recent log entry details
- **Statistics**: Real-time counts of files found and items parsed

### ⚙️ Settings & Persistence
- **Persistent Configuration**: Directory paths and preferences saved to database
- **Auto-Load**: Settings automatically restored on application restart
- **Customizable Refresh Interval**: Configure auto-refresh timing
- **Test Data Generator**: Built-in tool to generate sample logs for testing

## System Requirements

- **OS**: Windows 7 or later (64-bit)
- **Memory**: 2GB RAM minimum, 4GB+ recommended
- **Disk Space**: 200MB for application + space for database (varies by log volume)
- **.NET Runtime**: Included in the executable (self-contained)

## Installation

### Option 1: Direct Execution (Recommended)
1. Download `Tarklog.exe` from the distribution package
2. Place it anywhere on your system (Desktop, Documents, Program Files, etc.)
3. Double-click to run - no installation required!

### Option 2: Portable Installation
1. Extract the entire distribution folder
2. Run `Tarklog.exe` from the folder
3. Application data will be stored in `%APPDATA%\Tarklog\`

## Getting Started

### First Launch
1. Open Tarklog.exe
2. Go to **Settings & Directories** tab
3. Click **Browse** next to "Log Root Directory" and select where your main log files are stored
4. (Optional) Set "Log Storage Directory" for archived/backup logs
5. Click **Save Settings** to remember your directories

![Main Window - Settings Tab](imgs/Screenshot%202026-01-20%20144109.png)

*Tarklog main window showing the Settings & Directories tab*

### Scanning for Logs
1. With directories configured, click **Scan All Directories**
2. A progress dialog will appear showing:
   - **Phase 1**: Scanning subdirectories for log files
   - **Phase 2**: Parsing discovered log files and extracting data
3. Results appear in the directory contents panel showing:
   - Subdirectories found
   - Log files discovered
   - Number of parsed items per file
   - File sizes


### Viewing Results
- **Summary Tab**: Shows details of your most recent log entry
- **History & Analytics Tab**: Displays map distribution and timeline of recent entries
- **Settings & Directories Tab**: Browse and manage your log directories

![Summary Tab](imgs/Screenshot%202026-01-20%20144126.png)

*Summary tab displaying the most recent log entry details*

![Analytics Tab](imgs/Screenshot%202026-01-20%20144137.png)

*History & Analytics tab with map distribution pie chart and timeline*

![Directory Results](imgs/Screenshot%202026-01-20%20144148.png)

*Settings tab after scanning, showing discovered subdirectories and log files*

## Data Extracted from Logs

For each log entry, Tarklog captures:
- **Timestamp**: When the raid/event occurred
- **IP Address**: Server IP address
- **Map**: Game location (bigmap, shoreline, factory, etc.)
- **Raid ID**: Unique raid identifier
- **Data Center Code**: Server region code (e.g., DE-FRM, US-NYC)
- **Data Center Name**: Human-readable server location

## Log File Format

Tarklog expects log files matching the format:
```
YYYY.MM.DD_HH-mm-ss_IP_ADDRESS log_number application[_000].log
```

Examples:
- `2025.01.20_14-30-45_192.168.1.100 application_000.log`
- `2025.01.20_14-35-22_10.0.0.50 application.log`

Files are pipe-delimited (|) with format:
```
Timestamp|Version|Level|Module|MESSAGE containing Ip: X.X.X.X, Location: map, Sid: DC-CODE##...
```

## Database Storage

All application data is stored in a SQLite database located at:
```
%APPDATA%\Tarklog\tarklog.db
```

**Tables:**
- `Settings`: Application configuration (directories, preferences)
- `LogInstances`: Processed log files metadata
- `LogItems`: Parsed log data entries

## Testing

### Generate Sample Data
1. Go to **Settings & Directories** tab
2. Set a test directory path
3. Click **Generate Test Logs**
4. Click **Scan All Directories**
5. Verify that test files appear with parsed item counts

### Debugging
Enable Debug Output in Visual Studio (if running from source):
- Visual Studio → Debug → Windows → Output
- Look for `[DirectoryScanner]` and `[LogParser]` messages
- Shows detailed progress of file discovery and parsing

## Troubleshooting

### No files found during scan
- ✓ Verify the directory path is correct and accessible
- ✓ Ensure log files match the naming pattern (contains "application")
- ✓ Check file permissions - application needs read access

### Parsing shows 0 items
- ✓ Verify log file format is correct (pipe-delimited with "Ip:" marker)
- ✓ Check for encoding issues (should be UTF-8)
- ✓ Enable Debug Output to see parsing errors

### Application won't start
- ✓ Try running as Administrator
- ✓ Check if %APPDATA%\Tarklog\ folder exists and is writable
- ✓ Verify system has at least 2GB available RAM

### Database errors
- ✓ Delete `%APPDATA%\Tarklog\tarklog.db` to reset (will lose all saved data)
- ✓ Ensure %APPDATA% folder is accessible and writable

## Performance Tips

- **Organize Large Log Collections**: Split logs into separate root/storage directories
- **Regular Cleanup**: Archive old scans periodically to keep database responsive
- **SSD Recommended**: Faster scanning on SSDs vs. mechanical drives
- **Close Other Apps**: Frees up memory for processing large log volumes

## Features Coming Soon

- ✓ Folder management (auto-move old directories to storage)
- ✓ Export/reporting in CSV and PDF formats
- ✓ Advanced filtering and search capabilities
- ✓ Raid statistics and performance analytics
- ✓ Integration with raid planning tools
- ✓ Multi-instance log comparison

## Building from Source

Tarklog can be built for distribution in two formats:
- **Framework-dependent** (~2.9 MB) - Requires .NET 8.0 Desktop Runtime
- **Self-contained** (~70-80 MB) - Includes runtime, works on any Windows machine

### Quick Build

Run one of the provided build scripts from the project root:

**Windows Command Prompt:**
```batch
build.bat
```

**PowerShell:**
```powershell
.\build.ps1
```

Both scripts offer menu-driven options to build framework-dependent, self-contained, or both versions.

### Build Output

Compiled executables are created in:
- `dist\framework-dependent\Tarklog.exe`
- `dist\self-contained\Tarklog.exe`

### Prerequisites

- .NET 8.0 SDK
- Windows OS

For detailed build instructions, manual build commands, distribution packaging, and troubleshooting, see [BUILD_INSTRUCTIONS.md](BUILD_INSTRUCTIONS.md).

## Architecture

### Technology Stack
- **Framework**: .NET 8.0 WPF (Windows Presentation Foundation)
- **Database**: SQLite with Microsoft.Data.Sqlite provider
- **Charting**: OxyPlot for data visualization
- **File Dialog**: Windows Forms integration for folder browsing

### Core Components
- **DirectoryScanner**: Discovers log files using recursive directory scanning with regex pattern matching
- **LogParser**: Extracts structured data from unformatted log text using regex and string parsing
- **DatabaseManager**: Handles SQLite operations for persistence and querying
- **MainWindow**: WPF UI with tabbed interface for different views

## License & Support

For issues, feature requests, or contributions, please refer to the project repository.

## Version Info

- **Version**: 0.1.0
- **Release Date**: January 20, 2026
- **.NET Target**: net8.0-windows
- **Architecture**: win-x64 (64-bit Windows only)

---

**Tarklog** - Organize your raids, master your logs.
