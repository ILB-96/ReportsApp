using System.IO;
using System.Net.Http;

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
}
