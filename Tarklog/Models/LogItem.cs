using System;

namespace Tarklog.Models
{
    /// <summary>
    /// Represents a parsed log item extracted from log files
    /// </summary>
    public class LogItem
    {
        public int Id { get; set; }
        public int LogInstanceId { get; set; }
        public DateTime? Timestamp { get; set; }
        public string? IpAddress { get; set; }
        public string? Map { get; set; }
        public string? RaidId { get; set; }
        public string? DcCode { get; set; }
        public string? DcName { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
