using Reports.Services.GotoTech;

namespace Reports.Services.Reservation;

public sealed record ReservationReceipt
{
    public string? DriverName            { get; init; }
    public string? DriverId              { get; init; }
    public string? CarLicense            { get; init; }
    public string? CarType               { get; init; }
    public string? OriginAddress         { get; init; }
    public string? ReservationStartTime  { get; init; }
    public string? ReservationEndTime    { get; init; }
    public decimal ReservationCost       { get; init; }
    public string? ReservationId         { get; init; }
    public string? DistanceKm            { get; init; }
    public string? Brand               { get; init; }
}