namespace Reports.Services.Email.CustomerRequests;

public sealed record CustomerRequestEmailModel(
    string Company,
    string FullName,
    string IdNumber,
    string Email,
    AddressInfo Address,
    string CarNumber,
    string StartTime,
    string EndTime);