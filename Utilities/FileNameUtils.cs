using System.IO;

namespace Reports.Utilities
{
    public static class FileNameUtils
    {
        public static string SanitizeFileName(string input)
        {
            if (string.IsNullOrWhiteSpace(input)) return "Unnamed";

            var invalid = Path.GetInvalidFileNameChars();
            var safe = string.Concat(input.Split(invalid, StringSplitOptions.RemoveEmptyEntries)).Trim();

            return string.IsNullOrWhiteSpace(safe) ? "Unnamed" : safe;
        }
    }
}