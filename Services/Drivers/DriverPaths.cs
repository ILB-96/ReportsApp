using System.IO;
using Microsoft.Extensions.Options;
using Reports.Configuration;

namespace Reports.Services.Drivers;

public sealed class DriverPaths : IDriverPaths
{
    private readonly AppOptions _options;

    public DriverPaths(IOptions<AppOptions> options)
    {
        _options = options.Value;
    }

    public string StartFolder => _options.StartFolder;
    public string DriversFolder => _options.DriversFolder;

    public string DriversFolderPath =>
        Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            StartFolder,
            DriversFolder);

    public string DriversFile(string brand)
        => _options.DriversXLName.Replace("{brand}", brand);

    public int DriverCol(string key)
    {
        if (_options.DriversXLCols.TryGetValue(key, out var col))
            return col;

        throw new KeyNotFoundException($"Missing DriversXLCols mapping for '{key}' in appsettings.json.");
    }

    public int DriversLastColToClear =>
        _options.DriversXLCols.Count == 0 ? 0 : _options.DriversXLCols.Values.Max();
}