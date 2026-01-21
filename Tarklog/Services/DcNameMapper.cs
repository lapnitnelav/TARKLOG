namespace Tarklog.Services
{
    /// <summary>
    /// Provides mapping and grouping functionality for data center names
    /// </summary>
    public static class DcNameMapper
    {
        /// <summary>
        /// Parses a DC name (e.g., "DE-FRM") into country code and city code
        /// </summary>
        /// <param name="dcName">The DC name (e.g., "DE-FRM")</param>
        /// <returns>Tuple of (CountryCode, CityCode)</returns>
        public static (string CountryCode, string CityCode) ParseDcName(string? dcName)
        {
            if (string.IsNullOrWhiteSpace(dcName))
                return ("Unknown", "Unknown");

            var parts = dcName.Split('-');
            if (parts.Length >= 2)
            {
                return (parts[0].Trim().ToUpper(), parts[1].Trim().ToUpper());
            }

            return ("Unknown", "Unknown");
        }

        /// <summary>
        /// Gets the full country name from a country code
        /// </summary>
        /// <param name="countryCode">The country code (e.g., "DE")</param>
        /// <returns>The full country name or the code if not found</returns>
        public static string GetCountryName(string countryCode)
        {
            if (string.IsNullOrWhiteSpace(countryCode))
                return "Unknown";

            if (Data.CountryCodeMappings.Mappings.TryGetValue(countryCode, out var countryName))
                return countryName;

            return countryCode;
        }

        /// <summary>
        /// Gets a display name for a DC (e.g., "DE-FRM" -> "Germany (FRM)")
        /// </summary>
        /// <param name="dcName">The DC name</param>
        /// <returns>Display-friendly name</returns>
        public static string GetDisplayName(string? dcName)
        {
            var (countryCode, cityCode) = ParseDcName(dcName);

            if (countryCode == "Unknown")
                return "Unknown";

            string countryName = GetCountryName(countryCode);
            return $"{countryName} ({cityCode})";
        }
    }

    /// <summary>
    /// Represents server statistics grouped by country and city
    /// </summary>
    public class ServerStatistic
    {
        public string? CountryCode { get; set; }
        public string? CountryName { get; set; }
        public string? CityCode { get; set; }
        public string? DcName { get; set; }
        public int Count { get; set; }
        public string? Percentage { get; set; }
        public double PercentageValue { get; set; } = 0.0;
    }
}
