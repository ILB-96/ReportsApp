namespace Reports.Services.Email.OperationMail;

public sealed record OperationMailModel(
    string Brand,
    string AccountFullName,
    string ReportNumber,
    string ReportReason,
    string CarLicense,
    string ReportDate,
    string ReportAddress,
    string ReportPrice,
    string ReportCity);