# Tarklog Application - Implementation Plan

## **Project Overview**
Build a Windows Desktop application to manage and analyze log files with GUI panels for summary, history/stats, and settings.

---

## **Phase 1: Project Setup & Core Infrastructure**
1. Create a .NET WPF (Windows Presentation Foundation) project for the desktop GUI
2. Set up SQLite database schema:
   - Table 1: `LogInstances` (processed files tracking)
   - Table 2: `LogItems` (extracted parsed data with Ip, Map, RaidId, DcCode, DcName, Date)
3. Establish folder structure for LOGROOT, LOGSTORAGE, and configuration

---

## **Phase 2: Feature 1 - Directory Scanning & Folder Management**
1. Create a folder scanner to identify subfolders in LOGROOT
2. Determine "most recent" folder (by date/timestamp)
3. Implement safe file moving logic to LOGSTORAGE
4. Add duplicate detection/prevention
5. Log all operations for audit trail

---

## **Phase 3: Feature 2 - Log File Discovery & Parsing**
1. Search across LOGROOT and LOGSTORAGE for files matching pattern `YYYY.MM.DD_H-M-S_[IP] application_000.log`
2. Build file reader to iterate through log file lines
3. Implement pattern matching to find lines containing `Ip:`
4. Extract complete LOGITEM when pattern matches

---

## **Phase 4: Feature 3 - Data Extraction & Transformation**
1. Parse each LOGITEM by splitting on `|` character
2. Extract timestamp (first element)
3. Extract Sid field and split on `_` to get DCCODE and DCNAME
   - Example: `DE-FRM03G002_...` → DCCODE: `03`, DCNAME: `DE-FRM`
4. Extract Ip, Location (MAP), and shortId (RAIDID)
5. Validate extracted data

---

## **Phase 5: Feature 4 - Database Storage**
1. Create database connection manager
2. Implement insert logic for processed LOGINSTANCE files to avoid reprocessing
3. Store extracted LOGITEM data (Date, IpAddress, Map, RaidId, DcCode, DcName)
4. Add index creation for efficient querying

---

## **Phase 6: Feature 5 - Main Panel Display**
1. Query the most recent entry from the database
2. Design summary panel layout in WPF
3. Display the latest parsed LOGITEM with all extracted fields
4. Auto-refresh when new logs are processed

---

## **Phase 7: Feature 6 - Analytics & Visualization Panels**
1. Create history/stats panel with multiple views:
   - **Graph**: Count of each MAP value (pie/bar chart)
   - **Timeline**: Display 5/10/15 most recent entries with DCNAME, DCCODE, RAIDID
   - Add date range picker for timeline filtering
2. Implement charting library (e.g., OxyPlot or LiveCharts)

---

## **Phase 8: Settings Panel**
1. Create settings UI for:
   - LOGROOT directory path selection
   - LOGSTORAGE directory path selection
   - Refresh interval configuration
2. Persist settings to local configuration file

---

## **Phase 9: Integration & Polish**
1. Wire all features together
2. Add error handling and validation
3. Implement logging for debugging
4. Test across all panels and features
5. Add UI responsiveness (async operations)

---

## **Project Statistics**
- **Estimated Complexity**: Medium-High
- **Estimated Timeline**: 3-6 weeks depending on detail level
- **Primary Technology**: .NET WPF
- **Database**: SQLite
- **Target Platform**: Windows Desktop

