using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tarklog.Services
{
    /// <summary>
    /// Scans directories for log files
    /// </summary>
    public class DirectoryScanner
    {
        // Match: 2025.11.27_8-42-21_1.0.0.1.41967 application_000.log
        // Also match: 2025.11.12_8-29-43_0.16.9.5.40743 application.log
        private const string LogFilePattern = "*application_*.log";
        // Regex matches both: application_000.log and application.log
        private static readonly Regex ApplicationLogRegex = new Regex(@"application(_\d+)?\.log$", RegexOptions.IgnoreCase);

        /// <summary>
        /// Represents a found log file
        /// </summary>
        public class LogFileInfo
        {
            public string? FullPath { get; set; }
            public string? FileName { get; set; }
            public DateTime LastModified { get; set; }
            public long SizeBytes { get; set; }
        }

        /// <summary>
        /// Scans a directory and subdirectories for log files matching the pattern
        /// </summary>
        public static List<LogFileInfo> ScanDirectory(string rootPath)
        {
            var results = new List<LogFileInfo>();

            try
            {
                if (!Directory.Exists(rootPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Directory does not exist: {rootPath}");
                    return results;
                }

                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Starting scan of: {rootPath}");
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Looking for pattern: {LogFilePattern} with regex: {ApplicationLogRegex}");

                // Get all .log files from root and subdirectories
                var allLogFiles = Directory.EnumerateFiles(rootPath, "*.log", SearchOption.AllDirectories).ToList();
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Found {allLogFiles.Count} total .log files");
                
                // Also search for files containing "application" in the name
                var applicationFiles = Directory.EnumerateFiles(rootPath, "*application*.log", SearchOption.AllDirectories).ToList();
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Found {applicationFiles.Count} files with 'application' in name");

                // Combine and deduplicate
                var allCandidates = allLogFiles.Union(applicationFiles).Distinct().ToList();
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Total candidates after merge: {allCandidates.Count}");

                foreach (var filePath in allCandidates)
                {
                    try
                    {
                        var fileInfo = new FileInfo(filePath);
                        var fileName = fileInfo.Name;

                        // Filter to only files matching the pattern (application_XXX.log)
                        if (!ApplicationLogRegex.IsMatch(fileName))
                        {
                            System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Skipping (regex mismatch): {fileName}");
                            continue;
                        }

                        System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] MATCH - {filePath} (Size: {fileInfo.Length} bytes)");

                        results.Add(new LogFileInfo
                        {
                            FullPath = filePath,
                            FileName = fileInfo.Name,
                            LastModified = fileInfo.LastWriteTime,
                            SizeBytes = fileInfo.Length
                        });
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Error reading file info for {filePath}: {ex.Message}");
                    }
                }

                // Sort by last modified date (newest first)
                results = results.OrderByDescending(x => x.LastModified).ToList();
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Scan complete. Found {results.Count} matching log files in {rootPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Error scanning directory {rootPath}: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Gets subdirectories in a path, sorted by last modified date
        /// </summary>
        public static List<(string Name, string FullPath, DateTime LastModified, int FileCount)> GetSubdirectories(string rootPath)
        {
            var results = new List<(string, string, DateTime, int)>();

            try
            {
                if (!Directory.Exists(rootPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Directory does not exist: {rootPath}");
                    return results;
                }

                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Getting subdirectories of: {rootPath}");

                var directories = Directory.GetDirectories(rootPath);
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Found {directories.Length} subdirectories");

                foreach (var dirPath in directories)
                {
                    try
                    {
                        var dirInfo = new DirectoryInfo(dirPath);
                        // Count all .log files that match the application log pattern
                        var allLogFiles = Directory.EnumerateFiles(dirPath, "*.log", SearchOption.AllDirectories);
                        var applicationFiles = Directory.EnumerateFiles(dirPath, "*application*.log", SearchOption.AllDirectories);
                        var allCandidates = allLogFiles.Union(applicationFiles).Distinct();
                        var matchingFiles = allCandidates.Where(f => ApplicationLogRegex.IsMatch(Path.GetFileName(f))).Count();

                        System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Subdir '{dirInfo.Name}': found {matchingFiles} matching log files");
                        results.Add((dirInfo.Name, dirPath, dirInfo.LastWriteTime, matchingFiles));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Error reading directory info: {ex.Message}");
                    }
                }

                // Sort by last modified date (newest first)
                results = results.OrderByDescending(x => x.Item3).ToList();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Error getting subdirectories: {ex.Message}");
            }

            return results;
        }

        /// <summary>
        /// Gets the most recent directory
        /// </summary>
        public static string GetMostRecentDirectory(string rootPath)
        {
            var subdirs = GetSubdirectories(rootPath);
            return subdirs.FirstOrDefault().FullPath;
        }

        /// <summary>
        /// Moves old subdirectories to backup location, keeping only the most recent N directories
        /// </summary>
        /// <param name="logRootPath">Source directory containing subdirectories to clean</param>
        /// <param name="logStoragePath">Destination backup directory</param>
        /// <param name="keepCount">Number of most recent directories to keep (default: 5)</param>
        /// <returns>Number of directories moved</returns>
        public static int CleanupOldDirectories(string logRootPath, string logStoragePath, int keepCount = 5)
        {
            int movedCount = 0;

            try
            {
                if (!Directory.Exists(logRootPath))
                {
                    System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Log root path does not exist: {logRootPath}");
                    return 0;
                }

                // Ensure backup directory exists
                if (!Directory.Exists(logStoragePath))
                {
                    Directory.CreateDirectory(logStoragePath);
                    System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Created backup directory: {logStoragePath}");
                }

                // Get all subdirectories sorted by last modified date (newest first)
                var subdirectories = Directory.GetDirectories(logRootPath)
                    .Select(d => new DirectoryInfo(d))
                    .OrderByDescending(d => d.LastWriteTime)
                    .ToList();

                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Found {subdirectories.Count} subdirectories in {logRootPath}");
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Keeping {keepCount} most recent, moving the rest to {logStoragePath}");

                // Skip the most recent N directories and move the rest
                var directoriesToMove = subdirectories.Skip(keepCount).ToList();

                foreach (var dirInfo in directoriesToMove)
                {
                    try
                    {
                        string destinationPath = Path.Combine(logStoragePath, dirInfo.Name);

                        // If destination already exists, append timestamp to make it unique
                        if (Directory.Exists(destinationPath))
                        {
                            string timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
                            destinationPath = Path.Combine(logStoragePath, $"{dirInfo.Name}_{timestamp}");
                        }

                        System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Moving: {dirInfo.FullName} -> {destinationPath}");

                        // Check if source and destination are on the same drive
                        string sourceRoot = Path.GetPathRoot(dirInfo.FullName) ?? "";
                        string destRoot = Path.GetPathRoot(destinationPath) ?? "";

                        if (sourceRoot.Equals(destRoot, StringComparison.OrdinalIgnoreCase))
                        {
                            // Same drive - use fast move
                            Directory.Move(dirInfo.FullName, destinationPath);
                        }
                        else
                        {
                            // Different drives - copy then delete
                            CopyDirectory(dirInfo.FullName, destinationPath);
                            Directory.Delete(dirInfo.FullName, recursive: true);
                        }

                        movedCount++;
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Error moving directory {dirInfo.Name}: {ex.Message}");
                    }
                }

                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Cleanup complete. Moved {movedCount} directories.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[DirectoryScanner] Error during cleanup: {ex.Message}");
            }

            return movedCount;
        }

        /// <summary>
        /// Recursively copies a directory and all its contents
        /// </summary>
        private static void CopyDirectory(string sourceDir, string destDir)
        {
            // Create the destination directory
            Directory.CreateDirectory(destDir);

            // Copy all files
            foreach (string file in Directory.GetFiles(sourceDir))
            {
                string fileName = Path.GetFileName(file);
                string destFile = Path.Combine(destDir, fileName);
                File.Copy(file, destFile, overwrite: false);
            }

            // Recursively copy all subdirectories
            foreach (string subDir in Directory.GetDirectories(sourceDir))
            {
                string dirName = Path.GetFileName(subDir);
                string destSubDir = Path.Combine(destDir, dirName);
                CopyDirectory(subDir, destSubDir);
            }
        }
    }
}
