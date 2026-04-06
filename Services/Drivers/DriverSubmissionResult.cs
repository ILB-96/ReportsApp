namespace Reports.Services.Drivers;

public sealed class DriverSubmissionResult
{
    public required string ExcelPath { get; init; }
    public required string DriversFileName { get; init; }
    public required string AccountFolder { get; init; }
    public bool AgreementGenerated { get; init; }
}