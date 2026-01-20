namespace Tarklog.Models
{
    /// <summary>
    /// Represents map statistics for display in the analytics grid
    /// </summary>
    public class MapStatistic
    {
        public string? MapName { get; set; }
        public int Count { get; set; }
        public string Percentage { get; set; } = "0%";
        public double PercentageValue { get; set; } = 0.0;
    }
}
