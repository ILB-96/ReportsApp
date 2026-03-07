using System.Collections.ObjectModel;
using System.IO;
using Microsoft.Extensions.Options;
using Reports.Configuration;

namespace Reports.Services;

public sealed class AppConfig(IOptions<AppOptions> options, ChromeTabsStore store)
{
    private readonly AppOptions _options = options.Value;
    public ObservableCollection<string> TabUrls => store.TabUrls;

    public IReadOnlyList<string> ServiceTypes => _options.ServiceTypes;

    public string DriversFile(string brand)
        => _options.DriversXLName.Replace("{brand}", brand);

    public Uri BaseUri(string brand)
        => new Uri(_options.BaseURI.Replace("{brand}", brand));

    public string AgreementTemplate(string brand)
        => _options.AgreementPath.Replace("{brand}", brand);

    public string AutographTemplate(string brand)
        => _options.AutographPath.Replace("{brand}", brand);

    public string ReservationTemplate(string brand)
        => _options.ReservationPath.Replace("{brand}", brand);
    
    public string SignatureTemplate(string brand)
        => _options.SignaturePath.Replace("{brand}", brand);
    
    
    public string StartFolder => _options.StartFolder;
    public string DriversFolder => _options.DriversFolder;

    public string DriversFolderPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
        StartFolder, DriversFolder);
    
    public int DriverCol(string key)
    {
        if (_options.DriversXLCols.TryGetValue(key, out var col))
            return col;

        throw new KeyNotFoundException($"Missing DriversXLCols mapping for '{key}' in appsettings.json.");
    }

    public int DriversLastColToClear
        => _options.DriversXLCols.Count == 0 ? 0 : _options.DriversXLCols.Values.Max();

    public string ServiceTypeFromUrl(string url)
    {
        return _options.ServiceTypes
                          .FirstOrDefault(s => url.Contains(s, StringComparison.OrdinalIgnoreCase))
                      ?? string.Empty;
    }
}