using System;
using Microsoft.Data.Sqlite;
using System.IO;

namespace Tarklog.Database
{
    /// <summary>
    /// Manages SQLite database operations for Tarklog
    /// </summary>
    public class DatabaseManager
    {
        private readonly string _connectionString;
        private const string DbFileName = "tarklog.db";

        public DatabaseManager()
        {
            string appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string tarlogPath = Path.Combine(appDataPath, "Tarklog");
            
            if (!Directory.Exists(tarlogPath))
            {
                Directory.CreateDirectory(tarlogPath);
            }

            string dbPath = Path.Combine(tarlogPath, DbFileName);
            _connectionString = $"Data Source={dbPath};";
        }

        public void Initialize()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    CreateTables(connection);
                    connection.Close();
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to initialize database: {ex.Message}", ex);
            }
        }

        private void CreateTables(SqliteConnection connection)
        {
            // Create LogInstances table
            string createLogInstancesTable = @"
                CREATE TABLE IF NOT EXISTS LogInstances (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    FileName TEXT NOT NULL,
                    FilePath TEXT NOT NULL UNIQUE,
                    ProcessedAt DATETIME NOT NULL,
                    ItemCount INTEGER NOT NULL DEFAULT 0,
                    ProcessedLineCount INTEGER NOT NULL DEFAULT 0
                );";

            // Create LogItems table
            string createLogItemsTable = @"
                CREATE TABLE IF NOT EXISTS LogItems (
                    Id INTEGER PRIMARY KEY AUTOINCREMENT,
                    LogInstanceId INTEGER NOT NULL,
                    Timestamp DATETIME NOT NULL,
                    IpAddress TEXT,
                    Map TEXT,
                    RaidId TEXT,
                    DcCode TEXT,
                    DcName TEXT,
                    CreatedAt DATETIME NOT NULL DEFAULT CURRENT_TIMESTAMP,
                    FOREIGN KEY (LogInstanceId) REFERENCES LogInstances(Id)
                );";

            // Create Settings table
            string createSettingsTable = @"
                CREATE TABLE IF NOT EXISTS Settings (
                    Key TEXT PRIMARY KEY,
                    Value TEXT NOT NULL
                );";

            using (var command = connection.CreateCommand())
            {
                command.CommandText = createLogInstancesTable;
                command.ExecuteNonQuery();

                command.CommandText = createLogItemsTable;
                command.ExecuteNonQuery();

                command.CommandText = createSettingsTable;
                command.ExecuteNonQuery();

                // Create indexes for faster querying
                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_timestamp ON LogItems(Timestamp);";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_map ON LogItems(Map);";
                command.ExecuteNonQuery();

                command.CommandText = "CREATE INDEX IF NOT EXISTS idx_dcname ON LogItems(DcName);";
                command.ExecuteNonQuery();

                // Add ProcessedLineCount column if it doesn't exist (for existing databases)
                // Check if column exists first to avoid throwing exception
/*                 command.CommandText = "SELECT COUNT(*) FROM pragma_table_info('LogInstances') WHERE name='ProcessedLineCount';";
                var columnExists = Convert.ToInt32(command.ExecuteScalar()) > 0;

                if (!columnExists)
                {
                    command.CommandText = "ALTER TABLE LogInstances ADD COLUMN ProcessedLineCount INTEGER NOT NULL DEFAULT 0;";
                    command.ExecuteNonQuery();
                    System.Diagnostics.Debug.WriteLine("[DatabaseManager] Added ProcessedLineCount column to LogInstances table");
                } */
            }
        }

        public SqliteConnection GetConnection()
        {
            return new SqliteConnection(_connectionString);
        }

        /// <summary>
        /// Saves a setting value to the database
        /// </summary>
        public void SaveSetting(string key, string value)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    
                    // Try to insert, or update if exists
                    command.CommandText = @"
                        INSERT INTO Settings (Key, Value) VALUES (@key, @value)
                        ON CONFLICT(Key) DO UPDATE SET Value = @value";
                    
                    command.Parameters.AddWithValue("@key", key);
                    command.Parameters.AddWithValue("@value", value ?? "");
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving setting {key}: {ex.Message}");
            }
        }

        /// <summary>
        /// Retrieves a setting value from the database
        /// </summary>
        public string GetSetting(string key, string defaultValue = "")
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Value FROM Settings WHERE Key = @key";
                    command.Parameters.AddWithValue("@key", key);
                    
                    var result = command.ExecuteScalar();
                    return result?.ToString() ?? defaultValue;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error retrieving setting {key}: {ex.Message}");
                return defaultValue;
            }
        }

        /// <summary>
        /// Clears all log data from the database
        /// </summary>
        public void ClearAllLogs()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Delete all log items first (due to foreign key)
                    var command = connection.CreateCommand();
                    command.CommandText = "DELETE FROM LogItems";
                    command.ExecuteNonQuery();

                    // Then delete all log instances
                    command.CommandText = "DELETE FROM LogInstances";
                    command.ExecuteNonQuery();

                    System.Diagnostics.Debug.WriteLine("[DatabaseManager] All log data cleared");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error clearing log data: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Checks if a file has already been processed
        /// </summary>
        public bool IsFileProcessed(string filePath)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT COUNT(*) FROM LogInstances WHERE FilePath = @filePath";
                    command.Parameters.AddWithValue("@filePath", filePath);

                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result) > 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking if file is processed: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Saves a log instance (processed file) to the database
        /// </summary>
        public int SaveLogInstance(string fileName, string filePath, int itemCount)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = @"
                        INSERT INTO LogInstances (FileName, FilePath, ProcessedAt, ItemCount)
                        VALUES (@fileName, @filePath, @processedAt, @itemCount)";

                    command.Parameters.AddWithValue("@fileName", fileName);
                    command.Parameters.AddWithValue("@filePath", filePath);
                    command.Parameters.AddWithValue("@processedAt", DateTime.Now);
                    command.Parameters.AddWithValue("@itemCount", itemCount);

                    command.ExecuteNonQuery();

                    // Get the last inserted ID
                    command.CommandText = "SELECT last_insert_rowid()";
                    var result = command.ExecuteScalar();
                    return Convert.ToInt32(result);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving log instance: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Saves a collection of log items to the database
        /// </summary>
        public void SaveLogItems(int logInstanceId, List<Models.LogItem> items)
        {
            if (items.Count == 0) return;

            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();

                    // Use a transaction for better performance
                    using (var transaction = connection.BeginTransaction())
                    {
                        var command = connection.CreateCommand();
                        command.Transaction = transaction;

                        command.CommandText = @"
                            INSERT INTO LogItems (LogInstanceId, Timestamp, IpAddress, Map, RaidId, DcCode, DcName, CreatedAt)
                            VALUES (@logInstanceId, @timestamp, @ipAddress, @map, @raidId, @dcCode, @dcName, @createdAt)";

                        foreach (var item in items)
                        {
                            command.Parameters.Clear();
                            command.Parameters.AddWithValue("@logInstanceId", logInstanceId);
                            command.Parameters.AddWithValue("@timestamp", item.Timestamp ?? DateTime.Now);
                            command.Parameters.AddWithValue("@ipAddress", item.IpAddress ?? "");
                            command.Parameters.AddWithValue("@map", item.Map ?? "");
                            command.Parameters.AddWithValue("@raidId", item.RaidId ?? "");
                            command.Parameters.AddWithValue("@dcCode", item.DcCode ?? "");
                            command.Parameters.AddWithValue("@dcName", item.DcName ?? "");
                            command.Parameters.AddWithValue("@createdAt", DateTime.Now);

                            command.ExecuteNonQuery();
                        }

                        transaction.Commit();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error saving log items: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the most recent log item from the database
        /// </summary>
        public Models.LogItem? GetMostRecentLogItem()
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
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
                            return new Models.LogItem
                            {
                                Timestamp = reader.IsDBNull(0) ? null : (DateTime)reader[0],
                                IpAddress = reader.IsDBNull(1) ? null : (string)reader[1],
                                Map = reader.IsDBNull(2) ? null : (string)reader[2],
                                RaidId = reader.IsDBNull(3) ? null : (string)reader[3],
                                DcCode = reader.IsDBNull(4) ? null : (string)reader[4],
                                DcName = reader.IsDBNull(5) ? null : (string)reader[5]
                            };
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting most recent log item: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Gets the last processed line count for a file
        /// </summary>
        public int GetLastProcessedLineCount(string filePath)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT ProcessedLineCount FROM LogInstances WHERE FilePath = @filePath";
                    command.Parameters.AddWithValue("@filePath", filePath);

                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting processed line count for {filePath}: {ex.Message}");
                return 0;
            }
        }

        /// <summary>
        /// Updates the processed line count for a file
        /// </summary>
        public void UpdateProcessedLineCount(string filePath, int lineCount)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "UPDATE LogInstances SET ProcessedLineCount = @lineCount WHERE FilePath = @filePath";
                    command.Parameters.AddWithValue("@lineCount", lineCount);
                    command.Parameters.AddWithValue("@filePath", filePath);
                    command.ExecuteNonQuery();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error updating processed line count for {filePath}: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets the log instance ID by file path
        /// </summary>
        public int GetLogInstanceIdByPath(string filePath)
        {
            try
            {
                using (var connection = new SqliteConnection(_connectionString))
                {
                    connection.Open();
                    var command = connection.CreateCommand();
                    command.CommandText = "SELECT Id FROM LogInstances WHERE FilePath = @filePath";
                    command.Parameters.AddWithValue("@filePath", filePath);

                    var result = command.ExecuteScalar();
                    return result != null ? Convert.ToInt32(result) : 0;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error getting log instance ID for {filePath}: {ex.Message}");
                return 0;
            }
        }
    }
}
