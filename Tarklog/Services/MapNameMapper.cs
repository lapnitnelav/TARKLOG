using System.Collections.Generic;

namespace Tarklog.Services
{
    /// <summary>
    /// Provides mapping functionality to normalize map names for display
    /// </summary>
    public static class MapNameMapper
    {
        private static readonly Dictionary<string, string> MapNameMappings = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase)
        {
            // Customs mapping
            { "bigmap", "Customs" },

            // Reserve mapping
            { "RezervBase", "Reserve" },

            // Factory mappings (day, night, and sandbox variants)
            { "factory4_day", "Factory" },
            { "factory4_night", "Factory" },
            { "Sandbox", "Factory" },
            { "Sandbox_high", "Factory" },

            // Labs mapping
            { "laboratory", "Labs" }
        };

        /// <summary>
        /// Maps a raw map name to its display-friendly name
        /// </summary>
        /// <param name="rawMapName">The raw map name from the log file</param>
        /// <returns>The normalized display name, or the original name if no mapping exists</returns>
        public static string GetDisplayName(string? rawMapName)
        {
            if (string.IsNullOrWhiteSpace(rawMapName))
                return "Unknown";

            // Try to find a mapping
            if (MapNameMappings.TryGetValue(rawMapName, out var displayName))
                return displayName;

            // Return the original name if no mapping exists
            return rawMapName;
        }

        /// <summary>
        /// Gets all unique display names that maps can be normalized to
        /// </summary>
        public static IEnumerable<string> GetAllDisplayNames()
        {
            var uniqueNames = new HashSet<string>(MapNameMappings.Values)
            {
                "Unknown"
            };
            return uniqueNames;
        }
    }
}
