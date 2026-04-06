using System.IO;
using Reports.Services.Export;
using Reports.Services.Files;
using Reports.Services.Templates;
using Reports.Utilities;

namespace Reports.Services.Drivers;

public sealed class DriverSubmissionService : IDriverSubmissionService
{
    private readonly IDriverPaths _driverPaths;
    private readonly ITemplateCatalog _templateCatalog;
    private readonly IWordPdfExporter _pdfExporter;
    private readonly IFileDownloader _fileDownloader;
    private readonly IShellService _shellService;
    private readonly IDriversExportService _driversExportService;
    private readonly IDocxTemplateGenerator _docxTemplateGenerator;

    public DriverSubmissionService(
        IDriverPaths driverPaths,
        ITemplateCatalog templateCatalog,
        IWordPdfExporter pdfExporter,
        IFileDownloader fileDownloader,
        IShellService shellService,
        IDriversExportService driversExportService,
        IDocxTemplateGenerator docxTemplateGenerator)
    {
        _driverPaths = driverPaths;
        _templateCatalog = templateCatalog;
        _pdfExporter = pdfExporter;
        _fileDownloader = fileDownloader;
        _shellService = shellService;
        _driversExportService = driversExportService;
        _docxTemplateGenerator = docxTemplateGenerator;
    }

    public async Task<DriverSubmissionResult> SubmitAsync(DriverSubmission submission, CancellationToken ct = default)
    {
        ValidateSubmission(submission);

        Directory.CreateDirectory(_driverPaths.DriversFolderPath);

        var accountFolder = Path.Combine(
            _driverPaths.DriversFolderPath,
            $"{NormalizePlate(submission.CarLicense)} - {submission.AccountFullName}");

        Directory.CreateDirectory(accountFolder);

        await _fileDownloader.DownloadIfExistsAsync(submission.LicenseLink.Trim(), accountFolder, "license", ct);
        await _fileDownloader.DownloadIfExistsAsync(submission.PassportLink.Trim(), accountFolder, "passport", ct);

        _shellService.OpenDirectory(accountFolder);

        var excelPath = Path.Combine(_driverPaths.DriversFolderPath, _driverPaths.DriversFile(submission.ServiceType));
        var row = BuildExcelRow(submission);

        _driversExportService.AppendRow(excelPath, row);
        
        var shouldGenerateAgreement =
            !string.IsNullOrWhiteSpace(submission.ReservationNumber) ||
            submission.Brand == "autotel";

        if (shouldGenerateAgreement)
            await GenerateAgreementAsync(submission, accountFolder, ct);

        return new DriverSubmissionResult
        {
            ExcelPath = excelPath,
            DriversFileName = _driverPaths.DriversFile(submission.ServiceType),
            AccountFolder = accountFolder,
            AgreementGenerated = shouldGenerateAgreement
        };
    }

    private Dictionary<string, DriverRowValue> BuildExcelRow(DriverSubmission submission) => new()
    {
        ["CarLicense"]      = new() { Col = 1,  Val = NormalizePlate(submission.CarLicense) },
        ["ReportStartDate"] = new() { Col = 2,  Val = submission.ReportStartDate.Trim() },
        ["ReportEndDate"]   = new() { Col = 3,  Val = submission.ReportEndDate.Trim() },
        ["AccountFullName"] = new() { Col = 4,  Val = submission.AccountFullName.Trim() },
        ["DriverId"]        = new() { Col = 5,  Val = submission.DriverId.Trim() },
        ["Phone"]           = new() { Col = 6,  Val = submission.Phone.Trim() },
        ["DriverLicense"]   = new() { Col = 7,  Val = submission.DriverLicense.Trim() },
        ["Address"]         = new() { Col = 9,  Val = submission.Address.Trim() },
        ["House"]           = new() { Col = 10, Val = submission.House.Trim() },
        ["City"]            = new() { Col = 11, Val = submission.City.Trim() },
        ["Email"]           = new() { Col = 8,  Val = submission.Email.Trim() },
        ["PostalCode"]      = new() { Col = 12, Val = submission.PostalCode.Trim() },
    };

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
        var resourceName = _templateCatalog.AgreementTemplate(submission.Brand);

        await _docxTemplateGenerator.GenerateFromEmbeddedAsync(resourceName, docxPath, fields, ct);
        
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        await _pdfExporter.ExportAsync(docxPath, pdfPath, deleteDocx: true, ct);
    }

    private static string NormalizePlate(string plate)
        => plate.Trim().Replace("-", "");
}