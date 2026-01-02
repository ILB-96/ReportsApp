using Xceed.Document.NET;
using Xceed.Words.NET;

namespace Reports.Utilities
{
    public static class TokenMerge
    {
        public static void ReplaceTokens(DocX doc,
            IDictionary<string, string> fields,
            string prefix = "<<",
            string suffix = ">>",
            Action<StringReplaceTextOptions>? configure = null)
        {
            foreach (var (key, value) in fields)
            {
                var opts = new StringReplaceTextOptions
                {
                    SearchValue = $"{prefix}{key}{suffix}",
                    NewValue = value ?? string.Empty,
                    // Customize defaults here if you like:
                    // MatchCase = false,
                    // RegEx = false,
                    // TrackChanges = false,
                    // ReplaceFirst = false
                };

                configure?.Invoke(opts);
                doc.ReplaceText(opts);
            }
        }
    }
}