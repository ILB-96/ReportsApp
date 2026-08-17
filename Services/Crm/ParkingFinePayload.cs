namespace Reports.Services.Crm
{
public sealed record ParkingFinePayload(
    string? AccountId, // fans out to the 4 customer bindings (all same account in your sample)
    string? VehicleId,
    string VehiclePlateNumber,
    int ReportCost,
    string? ReportReason,
    string? ReservationNumber,
    string? CityId,           // gtg_ReportCity lookup
    string Municipality, // new_municipality (free text, e.g. "תל אביב")
    string ReportAddress, // c2g_reportaddress
    string ReportNumber, // c2g_reportnumber
    string ExecutionDate, // c2g_executiondate (UTC)
    int? Store,
    string? Description,
    string? ServiceType);
}