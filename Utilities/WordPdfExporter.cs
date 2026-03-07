using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Reports.Utilities
{
    public interface IWordPdfExporter
    {
        Task<string> ExportAsync(string docxPath, string pdfPath, bool deleteDocx = true, CancellationToken ct = default);
    }

    public sealed class WordPdfExporter : IWordPdfExporter
    {
        private const int WdExportFormatPdf = 17;
        private const int WdDoNotSaveChanges = 0;
        private const int WdExportOptimizeForPrint = 0;
        private const int WdExportAllDocument = 0;
        private const int WdExportDocumentContent = 0;
        private const int WdExportCreateHeadingBookmarks = 1;
        private const int WdAlertsNone = 0;

        public async Task<string> ExportAsync(string docxPath, string pdfPath, bool deleteDocx = true, CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(docxPath))
                throw new ArgumentException("DOCX path is required.", nameof(docxPath));

            if (string.IsNullOrWhiteSpace(pdfPath))
                throw new ArgumentException("PDF path is required.", nameof(pdfPath));

            if (!File.Exists(docxPath))
                throw new FileNotFoundException("DOCX file not found.", docxPath);

            var outputDirectory = Path.GetDirectoryName(pdfPath);
            if (string.IsNullOrWhiteSpace(outputDirectory))
                throw new InvalidOperationException("Invalid PDF output path.");

            Directory.CreateDirectory(outputDirectory);

            return await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                Type? wordType = Type.GetTypeFromProgID("Word.Application");
                if (wordType == null)
                    throw new InvalidOperationException("Microsoft Word is not installed or is not registered correctly.");

                object? wordApp = null;
                object? document = null;

                try
                {
                    wordApp = Activator.CreateInstance(wordType)
                              ?? throw new InvalidOperationException("Failed to start Microsoft Word.");

                    dynamic app = wordApp;
                    app.Visible = false;
                    app.DisplayAlerts = WdAlertsNone;

                    ct.ThrowIfCancellationRequested();

                    document = app.Documents.Open(
                        FileName: Path.GetFullPath(docxPath),
                        ReadOnly: true,
                        Visible: false
                    );

                    dynamic doc = document;

                    ct.ThrowIfCancellationRequested();

                    doc.ExportAsFixedFormat(
                        OutputFileName: Path.GetFullPath(pdfPath),
                        ExportFormat: WdExportFormatPdf,
                        OpenAfterExport: false,
                        OptimizeFor: WdExportOptimizeForPrint,
                        Range: WdExportAllDocument,
                        From: 1,
                        To: 1,
                        Item: WdExportDocumentContent,
                        IncludeDocProps: true,
                        KeepIRM: true,
                        CreateBookmarks: WdExportCreateHeadingBookmarks,
                        DocStructureTags: true,
                        BitmapMissingFonts: true,
                        UseISO19005_1: false
                    );

                    doc.Close(WdDoNotSaveChanges);
                    document = null;

                    app.Quit(WdDoNotSaveChanges);
                    wordApp = null;
                    
                    if (deleteDocx)
                    {
                        try
                        {
                            File.Delete(docxPath);
                        }
                        catch
                        {
                            // optional: log but don't crash
                        }
                    }
                    return pdfPath;

                }
                catch (Exception ex)
                {
                    throw new InvalidOperationException("Failed to export the document to PDF using Microsoft Word.", ex);
                }
                finally
                {
                    TryReleaseComObject(document);
                    TryReleaseComObject(wordApp);

                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                }
            }, ct);

        }

        private static void TryReleaseComObject(object? comObject)
        {
            if (comObject == null)
                return;

            try
            {
                System.Runtime.InteropServices.Marshal.FinalReleaseComObject(comObject);
            }
            catch
            {
            }
        }
    }
}