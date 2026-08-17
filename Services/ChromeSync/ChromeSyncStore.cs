using System.Collections.ObjectModel;

namespace Reports.Services.ChromeSync;

public sealed class ChromeSyncStore
{
    private readonly object _gate = new();

    public ObservableCollection<string> TabUrls { get; } = new();

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _cookiesByOrigin =
        new(StringComparer.OrdinalIgnoreCase);

    private readonly Dictionary<string, IReadOnlyDictionary<string, string>> _sessionStorageByOrigin =
        new(StringComparer.OrdinalIgnoreCase);

    private string? _betterwayRefreshToken;

    private static readonly HashSet<string> BoHosts = new(StringComparer.OrdinalIgnoreCase)
    {
        "car2gobo.gototech.co",
        "prodautotelbo.gototech.co",
    };

    public void ReplaceAll(
        IEnumerable<string> urls,
        IDictionary<string, Dictionary<string, string>> cookiesByOrigin,
        string? payloadBetterwayRefreshToken,
        IDictionary<string, Dictionary<string, string>>? sessionStorageByOrigin = null)
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

            _sessionStorageByOrigin.Clear();

            if (sessionStorageByOrigin is not null)
            {
                foreach (var kvp in sessionStorageByOrigin)
                {
                    if (!Uri.TryCreate(kvp.Key, UriKind.Absolute, out var uri))
                        continue;

                    if (!IsRelevantBoUrl(uri.ToString()))
                        continue;

                    var filtered = kvp.Value
                        .Where(x => !string.IsNullOrWhiteSpace(x.Key) && !string.IsNullOrWhiteSpace(x.Value))
                        .ToDictionary(x => x.Key, x => x.Value, StringComparer.OrdinalIgnoreCase);

                    if (filtered.Count == 0)
                        continue;

                    _sessionStorageByOrigin[uri.GetLeftPart(UriPartial.Authority)] = filtered;
                }
            }

            if (!string.IsNullOrWhiteSpace(payloadBetterwayRefreshToken))
                _betterwayRefreshToken = payloadBetterwayRefreshToken;
        }
    }

    public string? GetBetterwayRefreshToken()
    {
        lock (_gate)
            return _betterwayRefreshToken;
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

    public string? GetSessionStorageValue(string origin, string key)
    {
        if (string.IsNullOrWhiteSpace(origin) || string.IsNullOrWhiteSpace(key))
            return null;

        lock (_gate)
        {
            return _sessionStorageByOrigin.TryGetValue(origin, out var values)
                   && values.TryGetValue(key, out var value)
                ? value
                : null;
        }
    }

    public IReadOnlyDictionary<string, string> GetSessionStorageForOrigin(string origin)
    {
        if (string.IsNullOrWhiteSpace(origin))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        lock (_gate)
        {
            return _sessionStorageByOrigin.TryGetValue(origin, out var values)
                ? new Dictionary<string, string>(values, StringComparer.OrdinalIgnoreCase)
                : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }
    }

    private static bool IsRelevantCrmUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
               && uri.Host.Contains(".crm4.dynamics.com", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRelevantBoUrl(string url)
    {
        return Uri.TryCreate(url, UriKind.Absolute, out var uri) && BoHosts.Contains(uri.Host);
    }
}