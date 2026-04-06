namespace Reports.Services.Drivers;

public sealed class DriverSubmission
{
    public required string Brand { get; init; }
    public required string ServiceType { get; init; }

    public required string CarLicense { get; init; }
    public required string AccountFullName { get; init; }
    public required string DriverId { get; init; }
    public required string Phone { get; init; }
    public required string ReportStartDate { get; init; }
    public required string ReportEndDate { get; init; }
    public required string DriverLicense { get; init; }
    public required string Address { get; init; }
    public required string House { get; init; }
    public required string City { get; init; }
    public required string Email { get; init; }
    public required string PostalCode { get; init; }

    public required string ReservationNumber { get; init; }
    public required string CreatedOn { get; init; }
    public required string LicenseLink { get; init; }
    public required string PassportLink { get; init; }
}