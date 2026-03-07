using System.Text.Json;

namespace Reports.Utilities;

public static class JsonDictExtensions
{
    public static string? GetString(this Dictionary<string, object>? dict, string key)
    {
        if (dict is null) return null;
        if (!dict.TryGetValue(key, out var value)) return null;

        return value switch
        {
            JsonElement je => je.ValueKind switch
            {
                JsonValueKind.String => je.GetString(),
                JsonValueKind.Null => null,
                _ => je.ToString()
            },
            _ => value?.ToString()
        };
    }

    public static string GetFirstNonEmpty(this Dictionary<string, object>? dict, params string[] keys)
    {
        foreach (var key in keys)
        {
            var v = dict.GetString(key);
            if (!string.IsNullOrWhiteSpace(v))
                return v!;
        }
        return string.Empty;
    }
}