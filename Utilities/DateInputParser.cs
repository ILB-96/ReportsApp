using System.Globalization;

namespace Reports.Utilities
{
    public static class DateInputParser
    {
        private static readonly string[] Formats =
        {
            // user input formats
            "dd/MM/yyyy HH:mm",
            "HH:mm dd/MM/yyyy",

            // ISO 8601 — all variants the API might return
            "yyyy-MM-ddTHH:mm:ss",
            "yyyy-MM-ddTHH:mm:ss.f",
            "yyyy-MM-ddTHH:mm:ss.ff",
            "yyyy-MM-ddTHH:mm:ss.fff",
            "yyyy-MM-ddTHH:mm:ss.ffff",
            "yyyy-MM-ddTHH:mm:ss.fffff",
            "yyyy-MM-ddTHH:mm:ss.ffffff",
            "yyyy-MM-ddTHH:mm:ss.fffffffZ",
            "yyyy-MM-ddTHH:mm:ssZ",
            "yyyy-MM-ddTHH:mm",
            "yyyy-MM-dd",
        };

        public static DateTime Parse(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new ArgumentException("Date input must not be empty.", nameof(input));

            if (DateTime.TryParseExact(
                    input.Trim(),
                    Formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out var result))
                return result;

            // Last resort: let the runtime try — handles Z suffix, offsets, etc.
            if (DateTime.TryParse(input.Trim(), CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out result))
                return result;

            throw new FormatException($"Cannot parse date value: \"{input}\".");
        }

        public static bool TryParse(string input, out DateTime result)
        {
            result = default;
            if (string.IsNullOrWhiteSpace(input))
                return false;

            if (DateTime.TryParseExact(
                    input.Trim(),
                    Formats,
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out result))
                return true;

            return DateTime.TryParse(input.Trim(), CultureInfo.InvariantCulture,
                DateTimeStyles.RoundtripKind, out result);
        }

        public static DateTime UpperRound(DateTime dt, int minutes = 10)
        {
            var interval = TimeSpan.FromMinutes(minutes).Ticks;
            var remainder = dt.Ticks % interval;
            return remainder == 0
                ? new DateTime(dt.Ticks + interval, dt.Kind)
                : new DateTime(dt.Ticks - remainder + interval, dt.Kind);
        }
    }
}