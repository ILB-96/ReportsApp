using System.Collections.ObjectModel;

namespace Reports.Services.ChromeSync;

public sealed class ChromeSyncStore
{
    private readonly object _gate = new();

    public ObservableCollection<string> TabUrls { get; } = new();

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _cookiesByOrigin =
        new(StringComparer.OrdinalIgnoreCase);

    public void ReplaceAll(IEnumerable<string> urls, IDictionary<string, Dictionary<string, string>> cookiesByOrigin)
    {
        var filteredUrls = urls
            .Where(IsRelevantCrmUrl)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        for (int i = TabUrls.Count - 1; i >= 0; i--)
        {
            if (!filteredUrls.Contains(TabUrls[i], StringComparer.OrdinalIgnoreCase))
                TabUrls.RemoveAt(i);
        }

        foreach (var url in filteredUrls)
        {
            if (!TabUrls.Contains(url))
                TabUrls.Add(url);
        }

        lock (_gate)
        {
            _cookiesByOrigin.Clear();

            foreach (var kvp in cookiesByOrigin)
            {
                if (!Uri.TryCreate(kvp.Key, UriKind.Absolute, out var uri))
                    continue;

                if (!IsRelevantCrmUrl(uri.ToString()))
                    continue;

                var filteredCookies = kvp.Value
                    .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                    .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                if (filteredCookies.Count == 0)
                    continue;

                _cookiesByOrigin[uri.GetLeftPart(UriPartial.Authority)] = filteredCookies;
            }
        }
    }

    public IReadOnlyDictionary<string, string> GetCookiesForOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        lock (_gate)
        {
            return _cookiesByOrigin.TryGetValue(origin, out var cookies)
                ? new Dictionary<string, string>(cookies, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    public bool HasCookiesForUrl(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            return false;

        var origin = uri.GetLeftPart(UriPartial.Authority);

        lock (_gate)
        {
            return _cookiesByOrigin.TryGetValue(origin, out var cookies) && cookies.Count > 0;
        }
    }

    private static bool IsRelevantCrmUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Host.Contains(".crm4.dynamics.com", StringComparison.OrdinalIgnoreCase);
    }
}