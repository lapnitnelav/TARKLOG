namespace Tarklog.Models
{
    /// <summary>
    /// Represents a timeline entry for display in the Recent Entries grid
    /// </summary>
    public class TimelineEntry
    {
        public string? Timestamp { get; set; }
        public string? Map { get; set; }
        public string? Country { get; set; }
        public string? City { get; set; }
        public string? RaidId { get; set; }
    }
}
