using System.Globalization;

public static class DateInputParser
{
    private static readonly string[] Formats =
    {
        "dd/MM/yyyy HH:mm",  // 26/10/2026 01:13
        "HH:mm dd/MM/yyyy"   // 01:13 26/10/2026
    };

    public static DateTime Parse(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
            throw new ArgumentException("Date input must not be empty.", nameof(input));

        return DateTime.ParseExact(
            input.Trim(),
            Formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None);
    }

    public static bool TryParse(string input, out DateTime result)
    {
        result = default;
        if (string.IsNullOrWhiteSpace(input))
            return false;

        return DateTime.TryParseExact(
            input.Trim(),
            Formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out result);
    }
}