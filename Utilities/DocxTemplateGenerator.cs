using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Xceed.Words.NET;

namespace Reports.Utilities
{
    public static class DocxTemplateGenerator
    {
        /// <summary>
        /// Creates a DOCX from an embedded resource template (Reports.name.docx),
        /// merges tokens, saves to outputPath, returns the saved path.
        /// </summary>
        public static async Task<string> GenerateFromEmbeddedAsync(
            string embeddedResourceName,
            string outputPath,
            IDictionary<string, string> tokens,
            CancellationToken ct = default)
        {
            if (string.IsNullOrWhiteSpace(embeddedResourceName))
                throw new ArgumentException("Resource name is required.", nameof(embeddedResourceName));

            if (string.IsNullOrWhiteSpace(outputPath))
                throw new ArgumentException("Output path is required.", nameof(outputPath));

            // Ensure folder exists
            Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);

            // Do heavy IO + DocX work off the UI thread
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();

                var assembly = Assembly.GetExecutingAssembly();

                using var stream = assembly.GetManifestResourceStream(embeddedResourceName);
                if (stream == null)
                    throw new FileNotFoundException($"Template not found in resources: {embeddedResourceName}");

                // Unique temp file to avoid collisions
                var tempTemplatePath = Path.Combine(Path.GetTempPath(), $"agreement_template_{Guid.NewGuid():N}.docx");

                try
                {
                    using (var fileStream = File.Create(tempTemplatePath))
                        stream.CopyTo(fileStream);

                    using (var doc = DocX.Load(tempTemplatePath))
                    {
                        TokenMerge.ReplaceTokens(doc, tokens);

                        try
                        {
                            doc.SaveAs(outputPath);
                        }
                        catch (IOException ex)
                        {
                            // Common: user already has the doc open
                            throw new IOException("Please close the document and try again.", ex);
                        }
                    }
                }
                finally
                {
                    try { File.Delete(tempTemplatePath); } catch { /* ignore */ }
                }
            }, ct);

            return outputPath;
        }

        public static void OpenInShell(string path)
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = path,
                UseShellExecute = true
            });
        }
    }
}
