using System.Collections.Generic;

namespace Reports.Configuration;

public sealed class AppOptions
{
    public List<string> ServiceTypes { get; init; } = new();

    public string DriversXLName { get; init; } = string.Empty;

    public string BaseURI { get; init; } = string.Empty;

    public string AgreementPath { get; init; } = string.Empty;

    public string ReservationPath { get; init; } = string.Empty;
    
    public string SignaturePath { get; init; } = string.Empty;
    
    public string StartFolder { get; init; } = "Docs";
    public string DriversFolder { get; init; } = "Drivers";

    public Dictionary<string, int> DriversXLCols { get; init; } = new();
    
}
