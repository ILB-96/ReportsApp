using System.IO;
using System.Net.Http;
using PdfSharpCore.Pdf;
using PdfSharpCore.Pdf.IO;
using PdfSharpCore.Drawing;
using PdfSharpCore.Pdf;
namespace Reports.Services.Files;

public static class FileDownloader
{
    private static readonly HttpClient Http = new();

    public static async Task DownloadIfExistsAsync(string url, string targetFolder, string newNameWithoutExt)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) return;

        Directory.CreateDirectory(targetFolder);

        using var resp = await Http.GetAsync(uri, HttpCompletionOption.ResponseHeadersRead);
        resp.EnsureSuccessStatusCode();

        var mediaType = resp.Content.Headers.ContentType?.MediaType;
        var ext = GetExtensionFromContentType(mediaType);

        if (string.IsNullOrWhiteSpace(ext))
        {
            var bytes = await resp.Content.ReadAsByteArrayAsync();
            ext = SniffExtension(bytes) ?? "";
            await File.WriteAllBytesAsync(Path.Combine(targetFolder, newNameWithoutExt + ext), bytes);
            return;
        }

        var targetPath = Path.Combine(targetFolder, newNameWithoutExt + ext);
        await using var fs = File.Create(targetPath);
        await resp.Content.CopyToAsync(fs);
    }


    public static async Task<string?> MergePdfsAsync(
        IEnumerable<string> sourceFilePaths,
        string destinationPath,
        bool deleteSources = false,
        CancellationToken ct = default)
    {
        var paths = sourceFilePaths?.Where(File.Exists).ToList()
                    ?? throw new ArgumentNullException(nameof(sourceFilePaths));

        if (paths.Count == 0)
        {
            Console.WriteLine("MergePdfsAsync skipped: no existing source files.");
            return null;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);
        destinationPath = GetUniqueFilePath(destinationPath);

        await Task.Run(() =>
        {
            using var output = new PdfDocument();

            foreach (var path in paths)
            {
                ct.ThrowIfCancellationRequested();
                using var source = PdfReader.Open(path, PdfDocumentOpenMode.Import);
                for (var i = 0; i < source.PageCount; i++)
                    output.AddPage(source.Pages[i]);
            }

            output.Save(destinationPath);
        }, ct);

        if (deleteSources)
        {
            var destinationFull = Path.GetFullPath(destinationPath);

            foreach (var path in paths)
            {
                ct.ThrowIfCancellationRequested();

                // Never delete a source that is also our output.
                if (string.Equals(Path.GetFullPath(path), destinationFull, StringComparison.OrdinalIgnoreCase))
                    continue;

                try
                {
                    File.Delete(path);
                }
                catch (Exception ex)
                {
                    // Don't fail the whole operation if one file is locked or missing.
                    Console.WriteLine($"MergePdfsAsync: failed to delete '{path}': {ex.Message}");
                }
            }
        }

        return destinationPath;
    }

    private static string GetExtensionFromContentType(string? mediaType) => mediaType?.ToLowerInvariant() switch
    {
        "application/pdf" => ".pdf",
        "image/jpeg" => ".jpg",
        "image/png" => ".png",
        "image/webp" => ".webp",
        "image/heic" => ".heic",
        "image/heif" => ".heif",
        "image/heic-sequence" => ".heic",
        "image/heif-sequence" => ".heif",
        "application/msword" => ".doc",
        "application/vnd.openxmlformats-officedocument.wordprocessingml.document" => ".docx",
        _ => ""
    };

    private static string? SniffExtension(byte[] bytes)
    {
        if (bytes.Length < 12) return null;

        if (bytes[0] == 0x25 && bytes[1] == 0x50 && bytes[2] == 0x44 && bytes[3] == 0x46) return ".pdf";
        if (bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF) return ".jpg";
        if (bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47) return ".png";

        if (bytes[0] == (byte)'R' && bytes[1] == (byte)'I' && bytes[2] == (byte)'F' && bytes[3] == (byte)'F' &&
            bytes[8] == (byte)'W' && bytes[9] == (byte)'E' && bytes[10] == (byte)'B' && bytes[11] == (byte)'P')
            return ".webp";

        if (bytes[4] == (byte)'f' && bytes[5] == (byte)'t' && bytes[6] == (byte)'y' && bytes[7] == (byte)'p')
        {
            var brand = System.Text.Encoding.ASCII.GetString(bytes, 8, 4).ToLowerInvariant();
            if (brand is "heic" or "heix" or "hevc" or "hevx") return ".heic";
            if (brand is "heif" or "mif1" or "msf1") return ".heif";
        }

        return null;
    }
    public static async Task<string?> MoveIfExistsAsync(
        string? sourceFilePath,
        string destinationFolderPath,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            Console.WriteLine("MoveIfExistsAsync skipped: source path is empty.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(destinationFolderPath))
            throw new ArgumentException("Destination folder path cannot be null or empty.", nameof(destinationFolderPath));

        if (!File.Exists(sourceFilePath))
        {
            Console.WriteLine($"MoveIfExistsAsync skipped: file does not exist: {sourceFilePath}");
            return null;
        }

        Directory.CreateDirectory(destinationFolderPath);

        var fileName = Path.GetFileName(sourceFilePath);
        var destinationPath = Path.Combine(destinationFolderPath, fileName);
        destinationPath = GetUniqueFilePath(destinationPath);

        Console.WriteLine($"Moving file from '{sourceFilePath}' to '{destinationPath}'");

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();
            File.Move(sourceFilePath, destinationPath);
        }, ct);
        
        return destinationPath;
    }

    public static async Task<string?> ExtractPdfPagesAsync(
        string? sourceFilePath,
        string destinationFolderPath,
        int pageCount,
        bool deleteOriginal = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            Console.WriteLine("ExtractPdfPagesAsync skipped: source path is empty.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(destinationFolderPath))
            throw new ArgumentException("Destination folder path cannot be null or empty.", nameof(destinationFolderPath));

        if (!File.Exists(sourceFilePath))
        {
            Console.WriteLine($"ExtractPdfPagesAsync skipped: file does not exist: {sourceFilePath}");
            return null;
        }

        if (!string.Equals(Path.GetExtension(sourceFilePath), ".pdf", StringComparison.OrdinalIgnoreCase))
            return null;

        if (pageCount < 1)
            throw new ArgumentOutOfRangeException(nameof(pageCount), "Page count must be at least 1.");

        Directory.CreateDirectory(destinationFolderPath);

        var fileName = Path.GetFileName(sourceFilePath);
        var destinationPath = Path.Combine(destinationFolderPath, fileName);
        destinationPath = GetUniqueFilePath(destinationPath);

        Console.WriteLine($"Extracting first {pageCount} page(s) from '{sourceFilePath}' to '{destinationPath}'");

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            using var source = PdfReader.Open(sourceFilePath, PdfDocumentOpenMode.Import);
            using var output = new PdfDocument();

            var pagesToCopy = Math.Min(pageCount, source.PageCount);
            for (var i = 0; i < pagesToCopy; i++)
            {
                ct.ThrowIfCancellationRequested();
                output.AddPage(source.Pages[i]);
            }

            output.Save(destinationPath);
        }, ct);

        if (deleteOriginal)
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                File.Delete(sourceFilePath);
            }, ct);
        }
        return destinationPath;
    }

    public static async Task<string?> ImageToPdfAsync(
        string? sourceFilePath,
        string destinationPath,
        bool deleteOriginal = false,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(sourceFilePath))
        {
            Console.WriteLine("ImageToPdfAsync skipped: source path is empty.");
            return null;
        }

        if (!File.Exists(sourceFilePath))
        {
            Console.WriteLine($"ImageToPdfAsync skipped: file does not exist: {sourceFilePath}");
            return null;
        }

        if (string.Equals(Path.GetExtension(sourceFilePath), ".pdf", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"ImageToPdfAsync skipped: id pdf: {sourceFilePath}");
            return null;
        }


        Directory.CreateDirectory(Path.GetDirectoryName(destinationPath)!);

        await Task.Run(() =>
        {
            ct.ThrowIfCancellationRequested();

            using var doc = new PdfDocument();
            var page = doc.AddPage();
            page.Size = PdfSharpCore.PageSize.A4;

            using var gfx = XGraphics.FromPdfPage(page);
            using var image = XImage.FromFile(sourceFilePath);

            // Fit inside the page, preserving aspect ratio.
            var pageW = page.Width.Point;
            var pageH = page.Height.Point;
            var imageRatio = (double)image.PixelWidth / image.PixelHeight;
            var pageRatio = pageW / pageH;

            double drawW, drawH;
            if (imageRatio > pageRatio)
            {
                drawW = pageW;
                drawH = pageW / imageRatio;
            }
            else
            {
                drawH = pageH;
                drawW = pageH * imageRatio;
            }

            var x = (pageW - drawW) / 2;
            var y = (pageH - drawH) / 2;

            gfx.DrawImage(image, x, y, drawW, drawH);
            doc.Save(destinationPath);
        }, ct);
        if (deleteOriginal)
        {
            await Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested();
                File.Delete(sourceFilePath);
            }, ct);
        }

        return destinationPath;
    }
    private static string GetUniqueFilePath(string destinationPath)
    {
        if (!File.Exists(destinationPath))
            return destinationPath;

        var directory = Path.GetDirectoryName(destinationPath)
                        ?? throw new InvalidOperationException("Destination path has no directory.");

        var fileNameWithoutExtension = Path.GetFileNameWithoutExtension(destinationPath);
        var extension = Path.GetExtension(destinationPath);

        var counter = 1;
        string candidatePath;

        do
        {
            candidatePath = Path.Combine(
                directory,
                $"{fileNameWithoutExtension} ({counter}){extension}");
            counter++;
        }
        while (File.Exists(candidatePath));

        return candidatePath;
    }
    public static async Task<string?> DownloadAsPdfAsync(string link, string prefix, string accountFolder, CancellationToken ct)
    {
        // Source arrives either as a URL to download or a local path to move.
        // Only one produces a file per submission; the other no-ops.
        await DownloadIfExistsAsync(link.Trim(), accountFolder, prefix);
        var fileLocation = await MoveIfExistsAsync(link.Trim().Replace("\"", ""), accountFolder, ct);

        var targetPdf = Path.Combine(accountFolder, $"{prefix}.pdf");

        var source = Directory.EnumerateFiles(accountFolder)
            .FirstOrDefault(f =>
                Path.GetFileNameWithoutExtension(f).Contains(prefix, StringComparison.OrdinalIgnoreCase) &&
                !f.Equals(targetPdf, StringComparison.OrdinalIgnoreCase));

        if (string.IsNullOrWhiteSpace(source) && string.IsNullOrWhiteSpace(fileLocation))
            return null;

        if (!string.IsNullOrWhiteSpace(source) && Path.GetExtension(source).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
            File.Move(source, targetPdf, overwrite: true);
        else if(!string.IsNullOrWhiteSpace(source))
            await ImageToPdfAsync(source, targetPdf, deleteOriginal: true, ct);
        else
            await ImageToPdfAsync(fileLocation, targetPdf, deleteOriginal: true, ct);

        return targetPdf;
    }
}
