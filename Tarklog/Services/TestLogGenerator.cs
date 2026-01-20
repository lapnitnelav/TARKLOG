using System;
using System.IO;

namespace Tarklog.Services
{
    /// <summary>
    /// Generates sample log files for testing
    /// </summary>
    public class TestLogGenerator
    {
        public static void GenerateSampleLogs(string directory, int fileCount = 3, int entriesPerFile = 5)
        {
            try
            {
                if (!Directory.Exists(directory))
                    Directory.CreateDirectory(directory);

                var random = new Random();
                var maps = new[] { "bigmap", "smallmap", "factory", "customs", "interchange", "shoreline" };
                var dcNames = new[] { "DE-FRM", "US-NYC", "JP-TYO", "EU-LON", "APAC-SG" };
                var dcCodes = new[] { "03", "01", "02", "05", "06" };

                for (int f = 0; f < fileCount; f++)
                {
                    var timestamp = DateTime.Now.AddHours(-f);
                    var filename = $"{timestamp:yyyy.MM.dd_H-mm-ss}_1.0.0.1.41967 application_000.log";
                    var filepath = Path.Combine(directory, filename);

                    using (var writer = new StreamWriter(filepath))
                    {
                        // Write sample log entries
                        for (int i = 0; i < entriesPerFile; i++)
                        {
                            var entryTime = timestamp.AddMinutes(i * 10);
                            var map = maps[random.Next(maps.Length)];
                            var dcName = dcNames[random.Next(dcNames.Length)];
                            var dcCode = dcCodes[random.Next(dcCodes.Length)];
                            var ip = $"74.{random.Next(256)}.{random.Next(256)}.{random.Next(1, 255)}";
                            var raidId = GenerateRaidId();
                            var sid = $"{dcName}{dcCode}G002_691b328afccd7c5c890fabd2_17.11.25_17-34-50";

                            var logLine = $"{entryTime:yyyy-MM-dd HH:mm:ss.fff}|1.0.0.0.41787|Debug|application|TRACE-NetworkGameCreate profileStatus: 'Profileid: 5eacb6e52925b8162c347527, Status: Busy, RaidMode: Online, Ip: {ip}, Port: 17007, Location: {map}, Sid: {sid}, GameMode: deathmatch, shortId: {raidId}'";

                            writer.WriteLine(logLine);
                        }
                    }

                    System.Diagnostics.Debug.WriteLine($"Generated test log: {filepath}");
                }

                System.Diagnostics.Debug.WriteLine($"Generated {fileCount} test log files in {directory}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error generating test logs: {ex.Message}");
            }
        }

        private static string GenerateRaidId()
        {
            var chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var random = new Random();
            var result = "";
            for (int i = 0; i < 6; i++)
                result += chars[random.Next(chars.Length)];
            return result;
        }
    }
}
