namespace Reports.Utilities;

public static class StringExtensions
{
    public static string DigitsOnly(this string? s)
        => string.IsNullOrEmpty(s) ? string.Empty : new string(s.Where(char.IsDigit).ToArray());

    public static string NormalizePhone(this string? s)
        => (s ?? string.Empty).Replace("+", "").Trim();
}