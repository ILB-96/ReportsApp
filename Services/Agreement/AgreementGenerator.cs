using System.IO;
using Reports.Services.Templates;
using Reports.Utilities;


namespace Reports.Services.Agreement;

public interface IAgreementGenerator
{
    /// <summary>
    /// Generates the agreement docx from the brand template, exports it to PDF,
    /// and returns the produced PDF path.
    /// </summary>
    Task<string> GenerateAsync(
        string fullName,
        string createdOn,
        string brand,
        string outputFolder,
        CancellationToken ct = default);
}



public sealed class AgreementGenerator(
    ITemplateCatalog templateCatalog,
    IWordPdfExporter pdfExporter,
    IDocxTemplateGenerator docxTemplateGenerator)
    : IAgreementGenerator
{
    public async Task<string> GenerateAsync(
        string fullName,
        string createdOn,
        string brand,
        string outputFolder,
        CancellationToken ct = default)
    {
        var fields = new Dictionary<string, string>
        {
            ["Name"] = fullName,
            ["Date"] = createdOn
        };

        var safeName = FileNameUtils.SanitizeFileName(fullName);
        var docxPath = Path.Combine(outputFolder, $"Agreement - {safeName}.docx");
        var resourceName = templateCatalog.AgreementTemplate(brand);

        await docxTemplateGenerator.GenerateFromEmbeddedAsync(resourceName, docxPath, fields, ct);

        var pdfPath = Path.ChangeExtension(docxPath, ".pdf");
        return await pdfExporter.ExportAsync(docxPath, pdfPath, deleteDocx: true, ct);
    }
}