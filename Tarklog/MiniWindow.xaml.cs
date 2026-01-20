using System;
using System.Windows;
using System.Windows.Input;
using Microsoft.Data.Sqlite;
using Tarklog.Database;
using Tarklog.Services;

namespace Tarklog
{
    /// <summary>
    /// Compact mini window for non-intrusive monitoring
    /// </summary>
    public partial class MiniWindow : Window
    {
        private DatabaseManager? _dbManager;
        private System.Windows.Threading.DispatcherTimer? _autoRefreshTimer;

        public MiniWindow(DatabaseManager? dbManager)
        {
            InitializeComponent();
            _dbManager = dbManager;

            // Position window at top-right corner
            this.Left = SystemParameters.PrimaryScreenWidth - this.Width - 20;
            this.Top = 20;

            // Start auto-refresh
            StartAutoRefreshTimer();

            // Load initial data
            RefreshData();
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Allow dragging the window
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
            }
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            RefreshData();
        }

        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            // Signal to main window to restore
            this.DialogResult = true;
            this.Close();
        }

        private void StartAutoRefreshTimer()
        {
            int intervalSeconds = GetRefreshIntervalSeconds();

            if (intervalSeconds > 0)
            {
                _autoRefreshTimer = new System.Windows.Threading.DispatcherTimer();
                _autoRefreshTimer.Interval = TimeSpan.FromSeconds(intervalSeconds);
                _autoRefreshTimer.Tick += AutoRefreshTimer_Tick;
                _autoRefreshTimer.Start();
                System.Diagnostics.Debug.WriteLine($"[MiniWindow] Auto-refresh timer started with interval: {intervalSeconds} seconds");
            }
        }

        private void StopAutoRefreshTimer()
        {
            if (_autoRefreshTimer != null)
            {
                _autoRefreshTimer.Stop();
                _autoRefreshTimer.Tick -= AutoRefreshTimer_Tick;
                _autoRefreshTimer = null;
            }
        }

        private int GetRefreshIntervalSeconds()
        {
            if (_dbManager != null)
            {
                string intervalStr = _dbManager.GetSetting("RefreshInterval", "300");
                if (int.TryParse(intervalStr, out int interval))
                {
                    return interval;
                }
            }
            return 300;
        }

        private void AutoRefreshTimer_Tick(object? sender, EventArgs e)
        {
            RefreshData();
        }

        public void RefreshData()
        {
            try
            {
                using (var connection = _dbManager?.GetConnection())
                {
                    if (connection == null)
                    {
                        SetNoData();
                        return;
                    }

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

                            // Parse DC name to get country and city
                            var (countryCode, cityCode) = DcNameMapper.ParseDcName(dcName);
                            string countryName = DcNameMapper.GetCountryName(countryCode);
                            string serverLocation = string.IsNullOrEmpty(cityCode)
                                ? countryName
                                : $"{countryName}, {cityCode}";

                            // Parse and format the timestamp
                            string dateString = "Unknown";
                            if (reader["Timestamp"] != null && reader["Timestamp"] != DBNull.Value)
                            {
                                if (DateTime.TryParse(reader["Timestamp"].ToString(), out DateTime timestamp))
                                {
                                    dateString = timestamp.ToString("yyyy-MM-dd HH:mm");
                                }
                            }

                            // Update UI
                            MapTextBlock.Text = displayMapName;
                            DetailsTextBlock.Text = $"{serverLocation} • {ip}";
                            TimeTextBlock.Text = dateString;
                        }
                        else
                        {
                            SetNoData();
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[MiniWindow] Error refreshing data: {ex.Message}");
                SetNoData();
            }
        }

        private void SetNoData()
        {
            MapTextBlock.Text = "No raid data";
            DetailsTextBlock.Text = "";
            TimeTextBlock.Text = "";
        }

        protected override void OnClosed(EventArgs e)
        {
            StopAutoRefreshTimer();
            base.OnClosed(e);
        }
    }
}
