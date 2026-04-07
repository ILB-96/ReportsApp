using System.Collections.Generic;
using Microsoft.Extensions.Options;
using Reports.Configuration;

namespace Reports.Services.Crm;
public interface ICrmBrandResolver
{
    string ServiceTypeFromUrl(string url);
    Uri BaseUri(string brand);
    IReadOnlyList<string> ServiceTypes { get; }
}
public sealed class CrmBrandResolver : ICrmBrandResolver
{
    private readonly AppOptions _options;

    public CrmBrandResolver(IOptions<AppOptions> options)
    {
        _options = options.Value;
    }

    public IReadOnlyList<string> ServiceTypes => _options.ServiceTypes;

    public string ServiceTypeFromUrl(string url)
    {
        return _options.ServiceTypes
                   .FirstOrDefault(s => url.Contains(s, StringComparison.OrdinalIgnoreCase))
               ?? string.Empty;
    }

    public Uri BaseUri(string brand)
        => new(_options.BaseURI.Replace("{brand}", brand));
}