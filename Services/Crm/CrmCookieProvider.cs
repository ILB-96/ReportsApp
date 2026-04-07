using Reports.Services;
using Reports.Services.ChromeSync;

namespace Reports.Services.Crm;

public interface ICrmCookieProvider
{
    IReadOnlyDictionary<string, string> GetCookiesForUrl(string crmUrl);
}
public sealed class CrmCookieProvider : ICrmCookieProvider
{
    private static readonly string[] CookieOrder =
    {
        "CrmOwinAuth",
        "CrmOwinAuthC1",
        "CrmOwinAuthC2",
        "CrmOwinAuthC3",
        "CrmOwinAuthC4",
        "CrmOwinAuthC5"
    };

    private readonly ChromeSyncStore _store;

    public CrmCookieProvider(ChromeSyncStore store)
    {
        _store = store;
    }

    public IReadOnlyDictionary<string, string> GetCookiesForUrl(string crmUrl)
    {
        if (!Uri.TryCreate(crmUrl, UriKind.Absolute, out var uri))
            return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var origin = uri.GetLeftPart(UriPartial.Authority);
        var cookies = _store.GetCookiesForOrigin(origin);

        var ordered = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var name in CookieOrder)
        {
            if (cookies.TryGetValue(name, out var value) && !string.IsNullOrWhiteSpace(value))
            {
                ordered[name] = value;
            }
        }

        return ordered;
    }
}