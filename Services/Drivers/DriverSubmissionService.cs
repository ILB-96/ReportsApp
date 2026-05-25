using System.IO;
using Reports.Services.BetterwayApi;
using Reports.Services.Files;
using Reports.Services.Templates;
using Reports.Utilities;

namespace Reports.Services.Drivers;

public interface IDriverSubmissionService
{
    Task<DriverSubmissionResult> SubmitAsync(DriverSubmission submission, CancellationToken ct = default);
}
public sealed class DriverSubmissionService(
    IDriverPaths driverPaths,
    ITemplateCatalog templateCatalog,
    IWordPdfExporter pdfExporter,
    IFileDownloader fileDownloader,
    IShellService shellService,
    IDocxTemplateGenerator docxTemplateGenerator,
    IBetterwayDriverApi betterwayApi)
    : IDriverSubmissionService
{
    public async Task<DriverSubmissionResult> SubmitAsync(DriverSubmission submission, CancellationToken ct = default)
    {
        ValidateSubmission(submission);
        var profile = BetterwayProfileResolver.Resolve(submission.ServiceType);
        var payload = new DriverImportPayload(
            PlateNumber:        NormalizePlate(submission.CarLicense),
            ContractStartDate:  DateInputParser.Parse(submission.ReportStartDate),
            ContractEndDate:    DateInputParser.Parse(submission.ReportEndDate),
            Name:               submission.AccountFullName,
            IdNumber:           submission.DriverId,
            PhoneNumber:        submission.Phone,
            LicenseNumber:      submission.DriverLicense,
            Email:              submission.Email,
            Street:             submission.Address,
            HouseNumber:        submission.House,
            City:               submission.City,
            ZipCode:            submission.PostalCode);

        var result = await betterwayApi.CreateDriverAsync(payload, profile, ct);
        Directory.CreateDirectory(driverPaths.DriversFolderPath);

        var accountFolder = Path.Combine(
            driverPaths.DriversFolderPath,
            $"{NormalizePlate(submission.CarLicense)} - {submission.AccountFullName}");

        Directory.CreateDirectory(accountFolder);

        await fileDownloader.DownloadIfExistsAsync(submission.LicenseLink.Trim(), accountFolder, "license", ct);
        await fileDownloader.DownloadIfExistsAsync(submission.PassportLink.Trim(), accountFolder, "passport", ct);
        await fileDownloader.MoveIfExistsAsync(submission.ContractLink, accountFolder, ct);
        await fileDownloader.MoveIfExistsAsync(submission.CustomerLink, accountFolder, ct);
        await fileDownloader.MoveIfExistsAsync(submission.PickupLink, accountFolder, ct);
        await fileDownloader.MoveIfExistsAsync(submission.ReturnLink, accountFolder, ct);

        shellService.OpenDirectory(accountFolder);
        
        var shouldGenerateAgreement =
            !string.IsNullOrWhiteSpace(submission.ReservationNumber) ||
            submission.Brand == "autotel";

        if (shouldGenerateAgreement)
            await GenerateAgreementAsync(submission, accountFolder, ct);

        return new DriverSubmissionResult
        {
            ResponseBody = result,
            DriversFileName = driverPaths.DriversFile(submission.ServiceType),
            AccountFolder = accountFolder,
            AgreementGenerated = shouldGenerateAgreement
        };
    }
    
    private void ValidateSubmission(DriverSubmission submission)
    {
        if (submission.ReportStartDate == submission.ReportEndDate)
            throw new InvalidOperationException("שנה טווח חוזה.");

        var fields = new Dictionary<string, string>
        {
            ["CarLicense"] = submission.CarLicense,
            ["AccountFullName"] = submission.AccountFullName,
            ["DriverId"] = submission.DriverId,
            ["Phone"] = submission.Phone,
            ["ReportStartDate"] = submission.ReportStartDate,
            ["ReportEndDate"] = submission.ReportEndDate,
            ["DriverLicense"] = submission.DriverLicense,
            ["Address"] = submission.Address,
            ["House"] = submission.House,
            ["City"] = submission.City,
            ["Email"] = submission.Email,
            ["PostalCode"] = submission.PostalCode,
            ["ServiceType"] = submission.ServiceType,
            ["Brand"] = submission.Brand,
            ["CreatedOn"] = submission.CreatedOn
        };

        var missing = fields.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Value));
        if (!string.IsNullOrWhiteSpace(missing.Key))
            throw new InvalidOperationException($"חסר שדה: {missing.Key}.");
    }

    private async Task GenerateAgreementAsync(DriverSubmission submission, string accountFolder, CancellationToken ct)
    {
        var fields = new Dictionary<string, string>
        {
            ["Name"] = submission.AccountFullName,
            ["Date"] = submission.CreatedOn
        };

        var safeName = FileNameUtils.SanitizeFileName(submission.AccountFullName);
        var docxPath = Path.Combine(accountFolder, $"Agreement - {safeName}.docx");
        var resourceName = templateCatalog.AgreementTemplate(submission.Brand);

        await docxTemplateGenerator.GenerateFromEmbeddedAsync(resourceName, docxPath, fields, ct);
        
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        await pdfExporter.ExportAsync(docxPath, pdfPath, deleteDocx: true, ct);
    }

    private static string NormalizePlate(string plate)
        => plate.Trim().Replace("-", "");
}