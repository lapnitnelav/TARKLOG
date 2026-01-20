using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using Tarklog.Models;

namespace Tarklog.Services
{
    /// <summary>
    /// Parses log items from log file lines
    /// </summary>
    public class LogParser
    {
        // Pattern to find lines containing network game create entries
        private const string IpPattern = "Ip:";

        public LogItem ParseLogLine(string line, int logInstanceId)
        {
            try
            {
                if (!line.Contains(IpPattern))
                    return null;

                // Split by pipe character
                var parts = line.Split('|');
                if (parts.Length < 4)
                    return null;

                var logItem = new LogItem
                {
                    LogInstanceId = logInstanceId,
                    CreatedAt = DateTime.Now
                };

                // Extract timestamp (first part)
                if (DateTime.TryParse(parts[0], out var timestamp))
                {
                    logItem.Timestamp = timestamp;
                }

                // Parse the content part (typically the last part contains the key-value pairs)
                string content = line.Substring(line.IndexOf("Profileid:"));

                // Extract Ip (no quotes in actual format)
                logItem.IpAddress = ExtractValue(content, "Ip: ", ",");

                // Extract Location (Map)
                logItem.Map = ExtractValue(content, "Location: ", ",");

                // Extract shortId (RaidId) - ends with single quote
                logItem.RaidId = ExtractValue(content, "shortId: ", "'");

                // Extract and parse Sid for DcCode and DcName
                string sid = ExtractValue(content, "Sid: ", ",");
                if (!string.IsNullOrEmpty(sid))
                {
                    var dcInfo = ParseDcInfo(sid);
                    logItem.DcCode = dcInfo.Code;
                    logItem.DcName = dcInfo.Name;
                }

                return logItem;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error parsing log line: {ex.Message}");
                return null;
            }
        }

        private string ExtractValue(string content, string startMarker, string endMarker)
        {
            try
            {
                int startIndex = content.IndexOf(startMarker);
                if (startIndex == -1)
                    return null;

                startIndex += startMarker.Length;
                int endIndex = content.IndexOf(endMarker, startIndex);
                
                if (endIndex == -1)
                    endIndex = content.Length;

                return content.Substring(startIndex, endIndex - startIndex).Trim();
            }
            catch
            {
                return null;
            }
        }

        private (string Code, string Name) ParseDcInfo(string sid)
        {
            try
            {
                // Expected format: DE-FRM03G002_691b328afccd7c5c890fabd2_17.11.25_17-34-50
                if (string.IsNullOrEmpty(sid))
                    return (null, null);

                var parts = sid.Split('_');
                if (parts.Length == 0)
                    return (null, null);

                string dcPrefix = parts[0]; // DE-FRM03G002

                // Extract DcName (DE-FRM) and DcCode (03)
                // Pattern: letters-letters followed by digits
                var match = Regex.Match(dcPrefix, @"^([A-Z]+-[A-Z]+)(\d+)");
                
                if (match.Success)
                {
                    string dcName = match.Groups[1].Value; // DE-FRM
                    string dcCode = match.Groups[2].Value; // 03
                    return (dcCode, dcName);
                }

                return (null, null);
            }
            catch
            {
                return (null, null);
            }
        }

        /// <summary>
        /// Reads a log file and parses all matching log items
        /// </summary>
        public static List<LogItem> ParseLogFile(string filePath, int logInstanceId)
        {
            var results = new List<LogItem>();

            try
            {
                if (!File.Exists(filePath))
                {
                    System.Diagnostics.Debug.WriteLine($"File not found: {filePath}");
                    return results;
                }

                var parser = new LogParser();
                var lineNumber = 0;

                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    while ((line = reader.ReadLine()) != null)
                    {
                        lineNumber++;
                        try
                        {
                            var logItem = parser.ParseLogLine(line, logInstanceId);
                            if (logItem != null)
                            {
                                results.Add(logItem);
                                System.Diagnostics.Debug.WriteLine($"Parsed line {lineNumber}: {logItem.DcName} - {logItem.Map}");
                            }
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"Error parsing line {lineNumber}: {ex.Message}");
                        }
                    }
                }

                System.Diagnostics.Debug.WriteLine($"File {Path.GetFileName(filePath)} parsed: {results.Count} items found");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error reading log file {filePath}: {ex.Message}");
            }

            return results;
        }
    }
}
