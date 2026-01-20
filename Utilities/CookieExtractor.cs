using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Reports.Utilities
{
    

public static class CookieExtractor
{
    // Returns a dictionary with keys: CrmOwinAuth, CrmOwinAuthC1..C5 (if present)
    public static Dictionary<string, string> ExtractCrmOwinCookies(string rawCookieHeader)
    {
        if (string.IsNullOrWhiteSpace(rawCookieHeader))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        // Pattern: key=value; handles values containing any characters except semicolon
        var rx = new Regex(@"(?<=^|;\s*)(?<key>[^=;\s]+)=(?<val>[^;]*)", RegexOptions.Compiled);

        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CrmOwinAuth", "CrmOwinAuthC1", "CrmOwinAuthC2", "CrmOwinAuthC3", "CrmOwinAuthC4", "CrmOwinAuthC5",
            // If your environment uses ARRAffinity as well, uncomment:
            // "ARRAffinity", "ARRAffinitySameSite"
        };

        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in rx.Matches(rawCookieHeader))
        {
            var key = m.Groups["key"].Value.Trim();
            var val = m.Groups["val"].Value; // do not Trim(), values may be base64-like

            if (wanted.Contains(key))
            {
                // Some proxies encode '+' as space in certain paths; if you see issues, consider reversing it:
                // val = val.Replace(" ", "+");
                result[key] = val;
            }
        }

        return result;
    }
}
}
// Example usage:
// string cookieHeader = "<paste the entire Cookie header string here>";
// var picked = CookieExtractor.ExtractCrmOwinCookies(cookieHeader);
// foreach (var kv in picked) Console.WriteLine($"{kv.Key} = {kv.Value}");