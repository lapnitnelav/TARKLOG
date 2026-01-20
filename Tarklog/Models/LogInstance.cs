using System;

namespace Tarklog.Models
{
    /// <summary>
    /// Represents a processed log file instance
    /// </summary>
    public class LogInstance
    {
        public int Id { get; set; }
        public string? FileName { get; set; }
        public string? FilePath { get; set; }
        public DateTime ProcessedAt { get; set; }
        public int ItemCount { get; set; }
    }
}
