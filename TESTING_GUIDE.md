# Tarklog Testing Guide

## Quick Start for Testing Log File Discovery and Parsing

### Step 1: Launch the Application
1. Open Visual Studio and run the Tarklog application (Debug mode recommended)
2. Or run: `dotnet run` from the Tarklog directory

### Step 2: Enable Debug Output
To see detailed logging from the scanning and parsing operations:
1. In Visual Studio: **Debug → Windows → Output** (or Ctrl+Alt+O)
2. Make sure "Debug" is selected in the Output pane dropdown
3. You'll see colored debug messages with `[DirectoryScanner]` and other prefixes

### Step 3: Generate Test Logs
1. Go to the **Settings & Directories** tab
2. Click **Browse** next to "Log Root Directory"
3. Select a folder where you want to store test logs (or create a new folder like `C:\temp\test_logs`)
4. Click the **Generate Test Logs** button (orange button)
5. You should see: "Test logs generated! Click 'Scan Root' to view them."
6. Check the Debug Output window - you should see output like:
   ```
   [DirectoryScanner] Generated test log: C:\...\application_000.log
   [DirectoryScanner] Generated 3 test log files
   ```

### Step 4: Scan and View Results
1. Click the **Scan Root** button
2. Check the Debug Output - you should see:
   ```
   [DirectoryScanner] Starting scan of: C:\temp\test_logs
   [DirectoryScanner] Found X total .log files
   [DirectoryScanner] MATCH - C:\...\application_000.log (Size: XXXX bytes)
   ...
   [DirectoryScanner] Scan complete. Found X matching log files
   ```

3. In the UI, you should see:
   - **Subdirectories** list (if any subdirectories exist)
   - **Log Files Found** list showing:
     - FileName
     - Modified date
     - File size in KB
     - **Parsed items: X** (in green) - This is critical! Should be > 0

### What to Check

✅ **Files are being found**: You should see file names in the "Log Files Found" section
✅ **Parsing is working**: "Parsed items: X" should show a number > 0
✅ **Debug output**: Check Output window for detailed scan progress

### If Parsing Shows 0 Items

This means the log lines aren't matching the parsing pattern. Check Debug Output for:
- `[LogParser] Error parsing line` messages
- `[LogParser] File ... parsed: 0 items found`

### Troubleshooting

**No files found at all?**
- Make sure the Log Root path is set and valid
- Check that "Generate Test Logs" completed successfully
- Look for error messages in Debug Output starting with `[DirectoryScanner]`

**Files found but 0 items parsed?**
- The regex pattern `application_\d+\.log$` needs files ending in `application_000.log`, `application_001.log`, etc.
- Check the generated file names in the Debug Output
- The test generator creates files in the correct format, so this indicates a parsing issue with log line content

**Still no results?**
- Open Visual Studio Output window and search for `[DirectoryScanner]` or `[LogParser]` messages
- These will show exactly what's happening during the scan and parse operations

## Real Data Testing

Once test data works:
1. Browse to a folder containing your actual application log files
2. Click "Scan Root" 
3. Verify that log files are found and items are parsed
4. The file format should be: `YYYY.MM.DD_HH-mm-ss_IP application_XXX.log`

## Next Steps

After confirming file discovery and parsing works:
1. The parsed items need to be saved to the database
2. The Summary tab should display the most recent log entry
3. The Analytics tab should show map distribution and timeline

Check the IMPLEMENTATION_PLAN.md for the full feature roadmap.
