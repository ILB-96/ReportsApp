using System.Collections.Generic;

namespace Reports.Services.Crm;

public interface ICrmBrandResolver
{
    string ServiceTypeFromUrl(string url);
    Uri BaseUri(string brand);
    IReadOnlyList<string> ServiceTypes { get; }
}