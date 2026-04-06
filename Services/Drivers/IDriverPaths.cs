namespace Reports.Services.Drivers;

public interface IDriverPaths
{
    string StartFolder { get; }
    string DriversFolder { get; }
    string DriversFolderPath { get; }

    string DriversFile(string brand);
    int DriverCol(string key);
    int DriversLastColToClear { get; }
}