using System.Globalization;
using System.IO;
using System.Windows;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Reports.Services.Agreement;
using Reports.Services.BetterwayApi;
using Reports.Services.Files;
using Reports.Services.Reservation;
using Reports.Services.Templates;
using Reports.Utilities;
namespace Reports.Services.Drivers;

public interface IDriverSubmissionService
{
    Task<DriverSubmissionResult> SubmitAsync(DriverSubmission submission, ReservationReceipt reservation,  bool createDriver = true, CancellationToken ct = default);
}
public sealed class DriverSubmissionService(
    IDriverPaths driverPaths,
    ITemplateCatalog templateCatalog,
    IWordPdfExporter pdfExporter,
    IFileDownloader fileDownloader,
    IDocxTemplateGenerator docxTemplateGenerator,
    IBetterwayDriverApi betterwayDriverApi,
    IBetterwayVehicleSearch betterwayVehicleSearch,
    IAgreementGenerator agreementGenerator)
    : IDriverSubmissionService
{
    public async Task<DriverSubmissionResult> SubmitAsync(DriverSubmission submission, ReservationReceipt reservation,bool createDriver = true, CancellationToken ct = default)
    {
        var profile = BetterwayProfileResolver.Resolve(submission.ServiceType);
        
        var dtReservationEndTime = DateInputParser.Parse(reservation.ReservationEndTime ?? "");
        var dtReportEndDate = DateInputParser.Parse(submission.ReportEndDate);
        var contractEndDate =
            dtReportEndDate < dtReservationEndTime ? dtReservationEndTime : DateInputParser.UpperRound(dtReportEndDate);
        var payload = new DriverImportPayload(
            PlateNumber:        NormalizePlate(submission.CarLicense),
            ContractStartDate:  DateInputParser.Parse(submission.ReportStartDate),
            ContractEndDate:    contractEndDate,
            Name:               submission.AccountFullName,
            IdNumber:           submission.DriverId,
            PhoneNumber:        submission.Phone,
            LicenseNumber:      submission.DriverLicense,
            Email:              submission.Email,
            Street:             submission.Address,
            HouseNumber:        submission.House,
            City:               submission.City,
            ZipCode:            submission.PostalCode);

        var result = "";
        if (createDriver)
        {
            await ValidateSubmission(submission, profile);
            result = await betterwayDriverApi.CreateDriverAsync(payload, profile, ct);
            var success = !result.Contains("לא נמצא") && !result.Contains("קיים");
            if (!success)
            {
                return new DriverSubmissionResult
                {
                    ResponseBody = result,
                    DriversFileName = driverPaths.DriversFile(submission.ServiceType),
                    AccountFolder = driverPaths.DriversFolderPath,
                    AgreementGenerated = false
                };
            }
        }
        
        Directory.CreateDirectory(driverPaths.DriversFolderPath);

        var accountFolder = Path.Combine(
            driverPaths.DriversFolderPath,
            $"{submission.AccountFullName} - {NormalizePlate(submission.CarLicense)}");
        
        Directory.CreateDirectory(accountFolder);
        
        var pdfsToMerge = new List<string>();

        await fileDownloader.DownloadAsPdfAsync(submission.LicenseLink, "license", accountFolder, ct);

        await fileDownloader.DownloadAsPdfAsync(submission.PassportLink, "passport", accountFolder, ct);
        await CombinePassportWithEmailAsync(accountFolder, submission.Email, ct);

        await fileDownloader.ImageToPdfAsync(submission.CustomerLink.Replace("\"", ""), Path.Combine(accountFolder, "customer.pdf"),
            deleteOriginal: true, ct);
        await fileDownloader.MoveIfExistsAsync(submission.CustomerLink.Replace("\"", ""), accountFolder, ct);
        if (!String.IsNullOrWhiteSpace(reservation.ReservationId))
        {
            var reservationReceipt = await GenerateReservationAsync(reservation, accountFolder, ct);
            pdfsToMerge.Add(reservationReceipt);
        }
        var contract = await fileDownloader.ExtractPdfPagesAsync(submission.ContractLink.Replace("\"", ""), accountFolder, pageCount: 20,
            deleteOriginal: true, ct);
        if (String.IsNullOrWhiteSpace(contract))
        {
            contract = await fileDownloader.ImageToPdfAsync(submission.ContractLink.Replace("\"", ""), Path.Combine(accountFolder, "contract.pdf"),deleteOriginal: true, ct);
        }
        AddIfProduced(pdfsToMerge, contract);
        
        AddIfProduced(pdfsToMerge, await ConvertOrMoveAsync(submission.PickupLink, "pickup.pdf", accountFolder, ct));
        AddIfProduced(pdfsToMerge, await ConvertOrMoveAsync(submission.ReturnLink, "return.pdf", accountFolder, ct));
        
        
        var shouldGenerateAgreement =
            !string.IsNullOrWhiteSpace(submission.ReservationNumber) ||
            submission.Brand == "autotel";

        if (shouldGenerateAgreement)
        {
            var agreement = await agreementGenerator.GenerateAsync(submission.AccountFullName, submission.CreatedOn, submission.Brand, accountFolder, ct);
            pdfsToMerge.Add(agreement);
        }

        await fileDownloader.MergePdfsAsync(pdfsToMerge, Path.Combine(accountFolder, "merged.pdf"), deleteSources: true, ct);
        
        // shellService.OpenDirectory(accountFolder);
        


        return new DriverSubmissionResult
        {
            ResponseBody = result,
            DriversFileName = driverPaths.DriversFile(submission.ServiceType),
            AccountFolder = accountFolder,
            AgreementGenerated = shouldGenerateAgreement
        };
    }
    private async Task<string> GenerateReservationAsync(ReservationReceipt reservation, string accountFolder, CancellationToken ct)
    {
        var fields = new Dictionary<string, string>
        {
            ["Name"] = reservation.DriverName ?? "",
            ["Date"] = DateInputParser.Parse(reservation.ReservationEndTime).ToString("dd/MM/yyyy"),
            ["Id"] = reservation.DriverId ?? "",
            ["Car"] = reservation.CarType ?? "",
            ["CarId"] = reservation.CarLicense ?? "",
            ["Address"] = reservation.OriginAddress ?? "",
            ["Km"] = reservation.DistanceKm ?? "",
            ["Reservation"] = reservation.ReservationId ?? "",
            ["Start"] = DateInputParser.Parse(reservation.ReservationStartTime).ToString("dd/MM/yyyy HH:mm"),
            ["End"] = DateInputParser.Parse(reservation.ReservationEndTime).ToString("dd/MM/yyyy HH:mm"),
            ["Cost"] = reservation.ReservationCost.ToString(CultureInfo.InvariantCulture)
        };

        var safeName = FileNameUtils.SanitizeFileName(reservation.DriverName ?? "");
        var docxPath = Path.Combine(accountFolder, $"Reservation - {safeName}.docx");
        var resourceName = templateCatalog.ReservationTemplate(string.IsNullOrWhiteSpace(reservation.Brand) ? "goto" : reservation.Brand);

        await docxTemplateGenerator.GenerateFromEmbeddedAsync(resourceName, docxPath, fields, ct);
        
        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        return await pdfExporter.ExportAsync(docxPath, pdfPath, deleteDocx: true, ct);
    }
    private async Task ValidateSubmission(DriverSubmission submission, BetterwayProfile profile)
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
            ["CreatedOn"] = submission.CreatedOn
        };

        var missing = fields.FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Value));
        if (!string.IsNullOrWhiteSpace(missing.Key))
            throw new InvalidOperationException($"חסר שדה: {missing.Key}.");
        
        var start = ParseReportDate(submission.ReportStartDate, "תאריך התחלה");
        var end   = ParseReportDate(submission.ReportEndDate,   "תאריך סיום");
        if (start > end)
            throw new InvalidOperationException("תאריך ההתחלה מאוחר מתאריך הסיום.");
        
        var vehicle = await betterwayVehicleSearch.FindByPlateAsync(profile, NormalizePlate(submission.CarLicense));
        if (vehicle is null)
            throw new InvalidOperationException($"Car not found : {submission.CarLicense}");
        
        DateTime? rangeStart;
        DateTime? rangeEnd;
        string rangeLabel;

        if (vehicle.HasContract)
        {
            rangeStart = vehicle.ContractStartDate;
            rangeEnd   = vehicle.ContractEndDate;
            rangeLabel = "החוזה";
        }
        else
        {
            rangeStart = vehicle.OwnershipStartDate;
            rangeEnd   = vehicle.OwnershipEndDate;   // may be null = open-ended
            rangeLabel = "הבעלות";
        }

        // start must not precede the range start (when a start is known)
        if (rangeStart is { } rs && start < rs)
        {
            var ok = MessageBox.Show(
                $"Start date ({start:dd/MM/yyyy HH:mm}) is outside the {rangeLabel} range (starts {rs:dd/MM/yyyy}).\nContinue?",
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (ok != MessageBoxResult.Yes)
            {
                throw new InvalidOperationException(
                    $"תאריך ההתחלה ({start:dd/MM/yyyy HH:mm}) מחוץ לטווח {rangeLabel} (מתחיל {rs:dd/MM/yyyy}).");
            }
        }
        



        // end must not exceed the range end — but null end = open, so skip the check
        if (rangeEnd is { } re && end > re)
        {
            var ok = MessageBox.Show(
                $"End date ({end:dd/MM/yyyy HH:mm}) is outside the {rangeLabel} range (ends {re:dd/MM/yyyy}).\nContinue?",
                "Warning",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);
            if (ok != MessageBoxResult.Yes)
            {
                throw new InvalidOperationException(
                    $"תאריך ההתחלה ({end:dd/MM/yyyy HH:mm}) מחוץ לטווח {rangeLabel} (מסתיים {re:dd/MM/yyyy}).");
            }
        }
    }

    private static DateTime ParseReportDate(string raw, string fieldLabel)
    {
        if (DateInputParser.TryParse(raw, out var result))
            return result;
        
        throw new InvalidOperationException($"{fieldLabel} בפורמט לא תקין: \"{raw}\".");
        
    }
    
    private static void AddIfProduced(List<string> pdfs, string? path)
    {
        if (!string.IsNullOrWhiteSpace(path))
            pdfs.Add(path);
    }
    private static string Clean(string? link) => (link ?? string.Empty).Replace("\"", "");
    private async Task<string?> ConvertOrMoveAsync(string? link, string pdfName, string accountFolder, CancellationToken ct)
    {
        var cleaned = Clean(link);
        var pdf = await FileDownloader.ImageToPdfAsync(cleaned, Path.Combine(accountFolder, pdfName), deleteOriginal: true, ct);
        return string.IsNullOrWhiteSpace(pdf)
            ? await fileDownloader.MoveIfExistsAsync(cleaned, accountFolder, ct)
            : pdf;
    }
    private async Task<string?> CombinePassportWithEmailAsync(string accountFolder, string email, CancellationToken ct)
    {
        var passportPdf = Path.Combine(accountFolder, "passport.pdf");
        if (!File.Exists(passportPdf) || string.IsNullOrWhiteSpace(email))
            return null;

        // 1. One-page docx: the email as a big, centered single line.
        var emailDocx = Path.Combine(accountFolder, "email.docx");
        CreateEmailPageDocx(emailDocx, email.Trim());

        // 2. Render to PDF (deletes the docx).
        var emailPdf = Path.ChangeExtension(emailDocx, ".pdf");
        await pdfExporter.ExportAsync(emailDocx, emailPdf, deleteDocx: true, ct);

        // 3. Merge: passport (page 1) + email (page 2). Deletes both sources.
        var combinedPdf = Path.Combine(accountFolder, "passport_email.pdf");
        await fileDownloader.MergePdfsAsync([passportPdf, emailPdf], combinedPdf, deleteSources: true, ct);

        return combinedPdf;
    }

    private static void CreateEmailPageDocx(string path, string email)
    {
        // Keep it on one line: scale down as the address gets longer, still "big".
        var pt = email.Length switch
        {
            <= 20 => 44,
            <= 30 => 32,
            <= 45 => 24,
            _     => 18
        };
        var halfPoints = (pt * 2).ToString();

        using var doc = WordprocessingDocument.Create(path, WordprocessingDocumentType.Document);
        var main = doc.AddMainDocumentPart();

        main.Document = new Document(
            new Body(
                new Paragraph(
                    new ParagraphProperties(
                        new Justification { Val = JustificationValues.Center }),
                    new Run(
                        new RunProperties(
                            new RunFonts { Ascii = "Arial", HighAnsi = "Arial" },
                            new Bold(),
                            new FontSize { Val = halfPoints }),
                        new Text(email))),
                // Center the line vertically on the page as well.
                new SectionProperties(
                    new VerticalTextAlignmentOnPage { Val = VerticalJustificationValues.Center })));
    }

    private static string NormalizePlate(string plate)
        => plate.Trim().Replace("-", "");
}