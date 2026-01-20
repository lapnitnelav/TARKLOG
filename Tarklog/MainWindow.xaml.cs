using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using Microsoft.Data.Sqlite;
using System.Windows;
using System.Windows.Controls;
using OxyPlot;
using OxyPlot.Series;
using Tarklog.Database;
using Tarklog.Models;
using Tarklog.Services;

namespace Tarklog
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DatabaseManager? _dbManager;
        private const string SettingsKey = "TarlogSettings";

        // Date filter state
        private DateTime? _mapFilterStartDate = null;
        private DateTime? _mapFilterEndDate = null;
        private DateTime? _serverFilterStartDate = null;
        private DateTime? _serverFilterEndDate = null;

        public MainWindow()
        {
            InitializeComponent();
            InitializeApplication();
        }

        private void InitializeApplication()
        {
            try
            {
                _dbManager = new DatabaseManager();
                _dbManager.Initialize();
                LoadSettings();
                RefreshSummaryPanel();
                RefreshAnalyticsPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error initializing application: {ex.Message}", "Initialization Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshSummaryPanel();
            RefreshAnalyticsPanel();
        }

        private void RefreshSummaryPanel()
        {
            try
            {
                using (var connection = _dbManager?.GetConnection())
                {
                    if (connection == null) return;
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        SELECT Timestamp, IpAddress, Map, RaidId, DcCode, DcName
                        FROM LogItems
                        ORDER BY Timestamp DESC
                        LIMIT 1";

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            string rawMapName = reader["Map"]?.ToString() ?? "Unknown";
                            string displayMapName = MapNameMapper.GetDisplayName(rawMapName);
                            string raidId = reader["RaidId"]?.ToString() ?? "Unknown";
                            string ip = reader["IpAddress"]?.ToString() ?? "Unknown";
                            string dcName = reader["DcName"]?.ToString() ?? "Unknown";

                            // Parse and format the timestamp
                            string dateString = "Unknown";
                            if (reader["Timestamp"] != null && reader["Timestamp"] != DBNull.Value)
                            {
                                if (DateTime.TryParse(reader["Timestamp"].ToString(), out DateTime timestamp))
                                {
                                    dateString = timestamp.ToString("yyyy-MM-dd HH:mm");
                                }
                            }

                            // Format as: "Last raid (RaidID) on Map at Date played on DC Name (IP)"
                            SummaryTextBlock.Text = $"Last raid ({raidId}) on {displayMapName} at {dateString} played on {dcName} ({ip})";
                        }
                        else
                        {
                            ClearSummaryLabels();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing summary: {ex.Message}", "Refresh Error");
            }
        }

        private void RefreshAnalyticsPanel()
        {
            try
            {
                // Load map distribution chart
                LoadMapDistributionChart();

                // Load server distribution chart
                LoadServerDistributionChart();

                // Load timeline list
                LoadTimelineList();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error refreshing analytics: {ex.Message}", "Refresh Error");
            }
        }

        private void LoadMapDistributionChart()
        {
            try
            {
                var rawMapCounts = new Dictionary<string, int>();
                DateTime? minDate = null;
                DateTime? maxDate = null;

                using (var connection = _dbManager?.GetConnection())
                {
                    if (connection == null) return;
                    connection.Open();
                    var command = connection.CreateCommand();

                    // Build date filter
                    string dateFilter = BuildDateFilter(_mapFilterStartDate, _mapFilterEndDate);

                    // Get date range
                    command.CommandText = $@"
                        SELECT MIN(Timestamp) as MinDate, MAX(Timestamp) as MaxDate
                        FROM LogItems
                        WHERE Timestamp IS NOT NULL{dateFilter}";

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                var minDateValue = reader["MinDate"];
                                if (minDateValue is DateTime dt1)
                                    minDate = dt1;
                                else if (DateTime.TryParse(minDateValue?.ToString(), out var parsed1))
                                    minDate = parsed1;
                            }
                            if (!reader.IsDBNull(1))
                            {
                                var maxDateValue = reader["MaxDate"];
                                if (maxDateValue is DateTime dt2)
                                    maxDate = dt2;
                                else if (DateTime.TryParse(maxDateValue?.ToString(), out var parsed2))
                                    maxDate = parsed2;
                            }
                        }
                    }

                    // Get map counts
                    command.CommandText = $@"
                        SELECT Map, COUNT(*) as Count
                        FROM LogItems
                        WHERE Map IS NOT NULL{dateFilter}
                        GROUP BY Map
                        ORDER BY Count DESC";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawMap = reader["Map"]?.ToString() ?? "Unknown";
                            int count = (int)(long)reader["Count"];
                            rawMapCounts[rawMap] = count;
                        }
                    }
                }

                // Update date range label
                if (minDate.HasValue && maxDate.HasValue)
                {
                    string dateRange = minDate.Value.Date == maxDate.Value.Date
                        ? $"Date: {minDate.Value:yyyy-MM-dd}"
                        : $"Date Range: {minDate.Value:yyyy-MM-dd} to {maxDate.Value:yyyy-MM-dd}";
                    MapDateRangeLabel.Text = dateRange;
                }
                else
                {
                    MapDateRangeLabel.Text = "Date Range: No data";
                }

                // Map raw names to display names and aggregate counts
                var mapCounts = new Dictionary<string, int>();
                foreach (var kvp in rawMapCounts)
                {
                    string displayName = MapNameMapper.GetDisplayName(kvp.Key);
                    if (mapCounts.ContainsKey(displayName))
                        mapCounts[displayName] += kvp.Value;
                    else
                        mapCounts[displayName] = kvp.Value;
                }

                // Sort by count descending
                var sortedMapCounts = mapCounts.OrderByDescending(kvp => kvp.Value).ToList();

                // Calculate total for percentage
                int total = mapCounts.Values.Sum();

                // Update total count label
                MapTotalCountLabel.Text = $"(Total Raids: {total})";

                // Create pie chart
                var plotModel = new PlotModel { Title = "Map Distribution" };
                var pieSeries = new PieSeries();

                foreach (var kvp in sortedMapCounts)
                {
                    pieSeries.Slices.Add(new PieSlice(kvp.Key, kvp.Value));
                }

                plotModel.Series.Add(pieSeries);
                MapDistributionPlot.Model = plotModel;

                // Populate the statistics table
                var mapStatistics = new List<MapStatistic>();
                foreach (var kvp in sortedMapCounts)
                {
                    double percentage = total > 0 ? (kvp.Value * 100.0 / total) : 0;
                    mapStatistics.Add(new MapStatistic
                    {
                        MapName = kvp.Key,
                        Count = kvp.Value,
                        Percentage = $"{percentage:F1}%",
                        PercentageValue = percentage
                    });
                }

                MapStatsGrid.ItemsSource = mapStatistics;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading map distribution chart: {ex.Message}");
            }
        }

        private void LoadServerDistributionChart()
        {
            try
            {
                var rawDcCounts = new Dictionary<string, int>();
                DateTime? minDate = null;
                DateTime? maxDate = null;

                using (var connection = _dbManager?.GetConnection())
                {
                    if (connection == null) return;
                    connection.Open();
                    var command = connection.CreateCommand();

                    // Build date filter
                    string dateFilter = BuildDateFilter(_serverFilterStartDate, _serverFilterEndDate);

                    // Get date range
                    command.CommandText = $@"
                        SELECT MIN(Timestamp) as MinDate, MAX(Timestamp) as MaxDate
                        FROM LogItems
                        WHERE Timestamp IS NOT NULL{dateFilter}";

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                var minDateValue = reader["MinDate"];
                                if (minDateValue is DateTime dt1)
                                    minDate = dt1;
                                else if (DateTime.TryParse(minDateValue?.ToString(), out var parsed1))
                                    minDate = parsed1;
                            }
                            if (!reader.IsDBNull(1))
                            {
                                var maxDateValue = reader["MaxDate"];
                                if (maxDateValue is DateTime dt2)
                                    maxDate = dt2;
                                else if (DateTime.TryParse(maxDateValue?.ToString(), out var parsed2))
                                    maxDate = parsed2;
                            }
                        }
                    }

                    // Get DC counts
                    command.CommandText = $@"
                        SELECT DcName, COUNT(*) as Count
                        FROM LogItems
                        WHERE DcName IS NOT NULL AND DcName != ''{dateFilter}
                        GROUP BY DcName
                        ORDER BY Count DESC";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string dcName = reader["DcName"]?.ToString() ?? "Unknown";
                            int count = (int)(long)reader["Count"];
                            rawDcCounts[dcName] = count;
                        }
                    }
                }

                // Update date range label
                if (minDate.HasValue && maxDate.HasValue)
                {
                    string dateRange = minDate.Value.Date == maxDate.Value.Date
                        ? $"Date: {minDate.Value:yyyy-MM-dd}"
                        : $"Date Range: {minDate.Value:yyyy-MM-dd} to {maxDate.Value:yyyy-MM-dd}";
                    ServerDateRangeLabel.Text = dateRange;
                }
                else
                {
                    ServerDateRangeLabel.Text = "Date Range: No data";
                }

                // Calculate total for percentage
                int total = rawDcCounts.Values.Sum();

                // Update total count label
                ServerTotalCountLabel.Text = $"(Total Raids: {total})";

                // Create pie chart grouped by country
                var countryGroupedCounts = new Dictionary<string, int>();
                foreach (var kvp in rawDcCounts)
                {
                    var (countryCode, _) = DcNameMapper.ParseDcName(kvp.Key);
                    string countryName = DcNameMapper.GetCountryName(countryCode);

                    if (countryGroupedCounts.ContainsKey(countryName))
                        countryGroupedCounts[countryName] += kvp.Value;
                    else
                        countryGroupedCounts[countryName] = kvp.Value;
                }

                // Sort by count descending
                var sortedCountryCounts = countryGroupedCounts.OrderByDescending(kvp => kvp.Value).ToList();

                // Take top 8 countries and group the rest under "Misc"
                var topCountries = sortedCountryCounts.Take(8).ToList();
                var miscCount = sortedCountryCounts.Skip(8).Sum(kvp => kvp.Value);

                // Create pie chart
                var plotModel = new PlotModel { Title = "Server Distribution by Country" };
                var pieSeries = new PieSeries();

                foreach (var kvp in topCountries)
                {
                    pieSeries.Slices.Add(new PieSlice(kvp.Key, kvp.Value));
                }

                // Add Misc category if there are more than 8 countries
                if (miscCount > 0)
                {
                    pieSeries.Slices.Add(new PieSlice("Misc", miscCount));
                }

                plotModel.Series.Add(pieSeries);
                ServerDistributionPlot.Model = plotModel;

                // Populate the statistics table (grouped by DC, showing country and city)
                var serverStatistics = new List<ServerStatistic>();
                var sortedDcCounts = rawDcCounts.OrderByDescending(kvp => kvp.Value).ToList();

                foreach (var kvp in sortedDcCounts)
                {
                    var (countryCode, cityCode) = DcNameMapper.ParseDcName(kvp.Key);
                    string countryName = DcNameMapper.GetCountryName(countryCode);
                    double percentage = total > 0 ? (kvp.Value * 100.0 / total) : 0;

                    serverStatistics.Add(new ServerStatistic
                    {
                        CountryCode = countryCode,
                        CountryName = countryName,
                        CityCode = cityCode,
                        DcName = kvp.Key,
                        Count = kvp.Value,
                        Percentage = $"{percentage:F1}%",
                        PercentageValue = percentage
                    });
                }

                ServerStatsGrid.ItemsSource = serverStatistics;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading server distribution chart: {ex.Message}");
            }
        }

        private void LoadTimelineList()
        {
            try
            {
                int days = GetTimelineDays();
                var timelineEntries = new List<TimelineEntry>();
                DateTime startDate = DateTime.Now.AddDays(-days);

                using (var connection = _dbManager?.GetConnection())
                {
                    if (connection == null) return;
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = $@"
                        SELECT Timestamp, Map, DcName, DcCode, RaidId
                        FROM LogItems
                        WHERE Timestamp >= '{startDate:yyyy-MM-dd}'
                        ORDER BY Timestamp DESC";

                    using (var reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string rawMapName = reader["Map"]?.ToString() ?? "";
                            string displayMapName = MapNameMapper.GetDisplayName(rawMapName);

                            timelineEntries.Add(new TimelineEntry
                            {
                                Timestamp = reader["Timestamp"]?.ToString() ?? "N/A",
                                Map = displayMapName,
                                DcName = reader["DcName"]?.ToString() ?? "N/A",
                                DcCode = reader["DcCode"]?.ToString() ?? "N/A",
                                RaidId = reader["RaidId"]?.ToString() ?? "N/A"
                            });
                        }
                    }
                }

                TimelineGrid.ItemsSource = timelineEntries;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading timeline: {ex.Message}");
            }
        }

        private int GetTimelineDays()
        {
            if (TimelineDaysCombo != null)
            {
                string text = TimelineDaysCombo.Text;
                if (int.TryParse(text, out int days) && days > 0)
                {
                    return days;
                }
            }
            return 7; // Default to 7 days
        }

        private void ClearSummaryLabels()
        {
            SummaryTextBlock.Text = "No raid data available";
        }

        /// <summary>
        /// Builds a SQL WHERE clause filter for date range
        /// </summary>
        private string BuildDateFilter(DateTime? startDate, DateTime? endDate)
        {
            if (startDate.HasValue && endDate.HasValue)
            {
                return $" AND Timestamp >= '{startDate.Value:yyyy-MM-dd}' AND Timestamp <= '{endDate.Value:yyyy-MM-dd 23:59:59}'";
            }
            else if (startDate.HasValue)
            {
                return $" AND Timestamp >= '{startDate.Value:yyyy-MM-dd}'";
            }
            else if (endDate.HasValue)
            {
                return $" AND Timestamp <= '{endDate.Value:yyyy-MM-dd 23:59:59}'";
            }
            return "";
        }

        /// <summary>
        /// Calculates start date based on "Last N Days" quick filter
        /// </summary>
        private DateTime? GetQuickFilterStartDate(string filterText)
        {
            if (filterText.Contains("7 Days"))
                return DateTime.Now.AddDays(-7);
            else if (filterText.Contains("14 Days"))
                return DateTime.Now.AddDays(-14);
            else if (filterText.Contains("30 Days"))
                return DateTime.Now.AddDays(-30);
            else if (filterText.Contains("60 Days"))
                return DateTime.Now.AddDays(-60);
            else if (filterText.Contains("90 Days"))
                return DateTime.Now.AddDays(-90);
            else if (filterText.Contains("180 Days"))
                return DateTime.Now.AddDays(-180);
            else if (filterText.Contains("1 Year"))
                return DateTime.Now.AddYears(-1);
            else if (filterText.Contains("2 Years"))
                return DateTime.Now.AddYears(-2);
            return null; // All Time
        }

        // Map Distribution Date Filter Event Handlers
        private void MapDateFilterChanged(object sender, RoutedEventArgs e)
        {
            // Enable/disable controls based on radio button selection
            if (MapQuickFilterCombo != null && MapFromDatePicker != null && MapToDatePicker != null)
            {
                bool isQuickFilter = MapQuickFilterRadio.IsChecked == true;
                MapQuickFilterCombo.IsEnabled = isQuickFilter;
                MapFromDatePicker.IsEnabled = !isQuickFilter;
                MapToDatePicker.IsEnabled = !isQuickFilter;
            }
        }

        private void MapQuickFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MapQuickFilterCombo.SelectedItem is ComboBoxItem item && _dbManager != null)
            {
                string filterText = item.Content?.ToString() ?? "";
                _mapFilterStartDate = GetQuickFilterStartDate(filterText);
                _mapFilterEndDate = null; // Quick filters only set start date
                LoadMapDistributionChart();
            }
        }

        private void MapDatePickerChanged(object sender, SelectionChangedEventArgs e)
        {
            // Date pickers changed, but don't apply until Apply button clicked
        }

        private void MapApplyCustomFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_dbManager != null)
            {
                _mapFilterStartDate = MapFromDatePicker.SelectedDate;
                _mapFilterEndDate = MapToDatePicker.SelectedDate;
                LoadMapDistributionChart();
            }
        }

        // Server Distribution Date Filter Event Handlers
        private void ServerDateFilterChanged(object sender, RoutedEventArgs e)
        {
            // Enable/disable controls based on radio button selection
            if (ServerQuickFilterCombo != null && ServerFromDatePicker != null && ServerToDatePicker != null)
            {
                bool isQuickFilter = ServerQuickFilterRadio.IsChecked == true;
                ServerQuickFilterCombo.IsEnabled = isQuickFilter;
                ServerFromDatePicker.IsEnabled = !isQuickFilter;
                ServerToDatePicker.IsEnabled = !isQuickFilter;
            }
        }

        private void ServerQuickFilterCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ServerQuickFilterCombo.SelectedItem is ComboBoxItem item && _dbManager != null)
            {
                string filterText = item.Content?.ToString() ?? "";
                _serverFilterStartDate = GetQuickFilterStartDate(filterText);
                _serverFilterEndDate = null; // Quick filters only set start date
                LoadServerDistributionChart();
            }
        }

        private void ServerDatePickerChanged(object sender, SelectionChangedEventArgs e)
        {
            // Date pickers changed, but don't apply until Apply button clicked
        }

        private void ServerApplyCustomFilter_Click(object sender, RoutedEventArgs e)
        {
            if (_dbManager != null)
            {
                _serverFilterStartDate = ServerFromDatePicker.SelectedDate;
                _serverFilterEndDate = ServerToDatePicker.SelectedDate;
                LoadServerDistributionChart();
            }
        }

        private void BrowseLogRoot_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Log Root Directory";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LogRootTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void BrowseLogStorage_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                dialog.Description = "Select Log Storage Directory";
                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    LogStorageTextBox.Text = dialog.SelectedPath;
                }
            }
        }

        private void ScanAllDirectories_Click(object sender, RoutedEventArgs e)
        {
            // Validate that at least one directory is set
            if (string.IsNullOrWhiteSpace(LogRootTextBox.Text) && string.IsNullOrWhiteSpace(LogStorageTextBox.Text))
            {
                MessageBox.Show("Please select at least one directory (Log Root or Log Storage).", "No Directory Selected");
                return;
            }

            // Check if user wants to clear database before scanning
            if (ClearBeforeScanCheckBox.IsChecked == true)
            {
                var confirmResult = MessageBox.Show(
                    "This will delete ALL existing log entries from the database before scanning.\n\n" +
                    "This action cannot be undone.\n\n" +
                    "Do you want to continue?",
                    "Confirm Clear Database",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (confirmResult != MessageBoxResult.Yes)
                {
                    return;
                }

                try
                {
                    _dbManager?.ClearAllLogs();
                    MessageBox.Show("Database cleared successfully. Starting scan...", "Database Cleared", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error clearing database: {ex.Message}", "Clear Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            // Show progress dialog and scan in background
            var progressDialog = new ScanProgressDialog { Owner = this };
            progressDialog.Show();

            try
            {
                DirectoryStatusLabel.Text = "Scanning directories...";
                SubdirectoriesList.Items.Clear();
                LogFilesList.Items.Clear();

                int totalSubdirs = 0;
                int totalLogFiles = 0;
                int totalItemsParsed = 0;
                var allLogFiles = new List<dynamic>();

                // PHASE 1: Scan for log files
                progressDialog.SetPhase(1, "Scanning for application log files...");
                System.Windows.Forms.Application.DoEvents();

                var allDirectories = new List<(string path, string label)>();
                
                if (!string.IsNullOrWhiteSpace(LogRootTextBox.Text))
                    allDirectories.Add((LogRootTextBox.Text, "Log Root"));
                    
                if (!string.IsNullOrWhiteSpace(LogStorageTextBox.Text))
                    allDirectories.Add((LogStorageTextBox.Text, "Log Storage"));

                // Pre-scan: Count total subdirectories
                progressDialog.UpdateStatus("Counting subdirectories...");
                System.Windows.Forms.Application.DoEvents();

                int totalDirsToScan = 0;
                var allSubdirsList = new List<(string path, string label, List<(string name, string fullPath, DateTime lastModified, int fileCount)> subdirs)>();

                foreach (var (dirPath, dirLabel) in allDirectories)
                {
                    var subdirs = DirectoryScanner.GetSubdirectories(dirPath);
                    totalDirsToScan += subdirs.Count;
                    allSubdirsList.Add((dirPath, dirLabel, subdirs));
                }

                progressDialog.UpdateProgress(0, totalDirsToScan, "Starting scan...");
                System.Windows.Forms.Application.DoEvents();

                // Phase 1: Discover all log files
                int currentDirCount = 0;
                foreach (var (dirPath, dirLabel, subdirs) in allSubdirsList)
                {
                    foreach (var (name, fullPath, lastModified, fileCount) in subdirs)
                    {
                        currentDirCount++;
                        progressDialog.UpdateProgress(currentDirCount, totalDirsToScan, $"Scanning: [{dirLabel}] {name}");
                        System.Windows.Forms.Application.DoEvents();

                        SubdirectoriesList.Items.Add(new
                        {
                            Name = $"[{dirLabel}] {name} ({lastModified:yyyy-MM-dd HH:mm})",
                            FileCount = fileCount,
                            FullPath = fullPath
                        });
                        totalSubdirs++;
                    }

                    var logFiles = DirectoryScanner.ScanDirectory(dirPath);
                    foreach (var file in logFiles)
                    {
                        totalLogFiles++;
                        progressDialog.UpdateStats(totalLogFiles, totalItemsParsed);
                        System.Windows.Forms.Application.DoEvents();

                        allLogFiles.Add(new
                        {
                            File = file,
                            DirectoryLabel = dirLabel
                        });
                    }
                }

                // PHASE 2: Parse all log files and save to database
                progressDialog.SetPhase(2, "Parsing log files and saving to database...");
                System.Windows.Forms.Application.DoEvents();

                int filesProcessed = 0;
                int filesSkipped = 0;

                for (int i = 0; i < allLogFiles.Count; i++)
                {
                    dynamic fileEntry = allLogFiles[i];
                    var file = (DirectoryScanner.LogFileInfo)fileEntry.File;
                    string dirLabel = fileEntry.DirectoryLabel;

                    progressDialog.UpdateProgress(i + 1, allLogFiles.Count, $"Parsing: {file.FileName}");
                    System.Windows.Forms.Application.DoEvents();

                    // Check if file has already been processed
                    bool alreadyProcessed = _dbManager?.IsFileProcessed(file.FullPath) ?? false;

                    List<LogItem> logItems;
                    int logInstanceId = 0;

                    if (!alreadyProcessed)
                    {
                        // Parse the file with a temporary instance ID
                        logItems = LogParser.ParseLogFile(file.FullPath, 0);
                        totalItemsParsed += logItems.Count;

                        // Save to database
                        if (_dbManager != null && logItems.Count > 0)
                        {
                            // Save the log instance
                            logInstanceId = _dbManager.SaveLogInstance(file.FileName, file.FullPath, logItems.Count);

                            // Update all log items with the correct instance ID
                            foreach (var item in logItems)
                            {
                                item.LogInstanceId = logInstanceId;
                            }

                            // Save the log items
                            _dbManager.SaveLogItems(logInstanceId, logItems);
                            filesProcessed++;
                        }
                    }
                    else
                    {
                        // File already processed, just count the items
                        logItems = new List<LogItem>();
                        filesSkipped++;
                    }

                    progressDialog.UpdateStats(totalLogFiles, totalItemsParsed);

                    LogFilesList.Items.Add(new
                    {
                        FileName = file.FileName,
                        LastModified = file.LastModified,
                        SizeBytes = $"{file.SizeBytes / 1024.0:F2} KB",
                        ItemsFound = alreadyProcessed ? "Already processed" : logItems.Count.ToString(),
                        FullPath = file.FullPath,
                        Source = $"[{dirLabel}]"
                    });
                }

                // Sort combined results by date (newest first)
                var sortedLogFiles = new List<dynamic>();
                foreach (var item in LogFilesList.Items.Cast<dynamic>())
                {
                    sortedLogFiles.Add(item);
                }
                sortedLogFiles = sortedLogFiles.OrderByDescending(x => x.LastModified).ToList();
                
                LogFilesList.Items.Clear();
                foreach (var item in sortedLogFiles)
                {
                    LogFilesList.Items.Add(item);
                }

                progressDialog.UpdateStatus("Scan complete!");
                System.Threading.Thread.Sleep(500);

                DirectoryStatusLabel.Text = $"[COMPLETE] Found {totalSubdirs} subdirectories, {totalLogFiles} log files, {totalItemsParsed} parsed items ({filesProcessed} saved, {filesSkipped} already processed)";

                // Refresh the UI panels to show the newly saved data
                RefreshSummaryPanel();
                RefreshAnalyticsPanel();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error scanning directories: {ex.Message}", "Scan Error");
                DirectoryStatusLabel.Text = "Scan failed";
            }
            finally
            {
                progressDialog.Close();
            }
        }

        private void CleanupOldDirectories_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Validate directories are configured
                if (string.IsNullOrWhiteSpace(LogRootTextBox.Text))
                {
                    MessageBox.Show("Please configure the Log Root Directory first.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                if (string.IsNullOrWhiteSpace(LogStorageTextBox.Text))
                {
                    MessageBox.Show("Please configure the Log Storage Directory first.", "Configuration Required", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Check if Log Root exists
                if (!Directory.Exists(LogRootTextBox.Text))
                {
                    MessageBox.Show("Log Root Directory does not exist.", "Directory Not Found", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Count directories that will be moved
                var subdirectories = Directory.GetDirectories(LogRootTextBox.Text);
                int totalDirs = subdirectories.Length;
                int dirsToMove = Math.Max(0, totalDirs - 5);

                if (dirsToMove == 0)
                {
                    MessageBox.Show($"Found {totalDirs} subdirectories. No cleanup needed (keeping most recent 5).", "No Action Required", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                // Ask for confirmation
                var result = MessageBox.Show(
                    $"This will move {dirsToMove} old subdirectories to the backup location, keeping the 5 most recent.\n\n" +
                    $"Total subdirectories: {totalDirs}\n" +
                    $"Will keep: 5\n" +
                    $"Will move: {dirsToMove}\n\n" +
                    $"From: {LogRootTextBox.Text}\n" +
                    $"To: {LogStorageTextBox.Text}\n\n" +
                    $"Do you want to continue?",
                    "Confirm Cleanup",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question
                );

                if (result != MessageBoxResult.Yes)
                {
                    return;
                }

                // Perform the cleanup
                DirectoryStatusLabel.Text = "Cleaning up old directories...";
                DirectoryStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);

                int movedCount = DirectoryScanner.CleanupOldDirectories(LogRootTextBox.Text, LogStorageTextBox.Text, keepCount: 5);

                DirectoryStatusLabel.Text = $"Cleanup complete! Moved {movedCount} directories to backup.";
                DirectoryStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);

                MessageBox.Show(
                    $"Successfully moved {movedCount} directories to backup location.\n\n" +
                    $"The 5 most recent subdirectories have been kept in the Log Root.",
                    "Cleanup Complete",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information
                );

                // Refresh the directory view
                SubdirectoriesList.Items.Clear();
                var remainingDirs = DirectoryScanner.GetSubdirectories(LogRootTextBox.Text);
                foreach (var (name, fullPath, lastModified, fileCount) in remainingDirs)
                {
                    SubdirectoriesList.Items.Add(new
                    {
                        Name = $"{name} ({lastModified:yyyy-MM-dd HH:mm})",
                        FileCount = fileCount
                    });
                }
            }
            catch (Exception ex)
            {
                DirectoryStatusLabel.Text = "Cleanup failed";
                DirectoryStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                MessageBox.Show($"Error during cleanup: {ex.Message}", "Cleanup Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveSettings_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_dbManager == null) return;

                // Save directory paths
                _dbManager.SaveSetting("LogRootDirectory", LogRootTextBox.Text);
                _dbManager.SaveSetting("LogStorageDirectory", LogStorageTextBox.Text);
                _dbManager.SaveSetting("RefreshInterval", RefreshIntervalTextBox.Text);
                
                MessageBox.Show("Settings saved successfully.", "Settings", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error");
            }
        }

        private void CancelSettings_Click(object sender, RoutedEventArgs e)
        {
            LoadSettings();
        }

        private void GenerateTestLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(LogRootTextBox.Text))
                {
                    MessageBox.Show("Please select a Log Root directory first.", "Setup Required");
                    return;
                }

                TestStatusLabel.Text = "Generating test logs...";
                TestStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);

                TestLogGenerator.GenerateSampleLogs(LogRootTextBox.Text, fileCount: 3, entriesPerFile: 10);

                TestStatusLabel.Text = "Test logs generated! Click 'Scan Root' to view them.";
                TestStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);

                System.Diagnostics.Debug.WriteLine("Test logs generated successfully");
            }
            catch (Exception ex)
            {
                TestStatusLabel.Text = $"Error: {ex.Message}";
                TestStatusLabel.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                MessageBox.Show($"Error generating test logs: {ex.Message}", "Generation Error");
            }
        }

        private void LoadSettings()
        {
            if (_dbManager == null) return;

            // Load directory paths from database
            LogRootTextBox.Text = _dbManager.GetSetting("LogRootDirectory", "");
            LogStorageTextBox.Text = _dbManager.GetSetting("LogStorageDirectory", "");
            RefreshIntervalTextBox.Text = _dbManager.GetSetting("RefreshInterval", "30");
        }

        private void TimelineDaysCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Update when dropdown selection changes (not when typing)
            if (TimelineDaysCombo != null && TimelineDaysCombo.SelectedItem is ComboBoxItem && _dbManager != null)
            {
                LoadTimelineList();
            }
        }

        private void TimelineApplyFilter_Click(object sender, RoutedEventArgs e)
        {
            // Apply filter when button is clicked (useful when user types a custom value)
            if (_dbManager != null)
            {
                LoadTimelineList();
            }
        }
    }
}