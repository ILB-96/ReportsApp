namespace Reports.Services.Files;

public sealed class FileDownloaderService : IFileDownloader
{
    public Task DownloadIfExistsAsync(string url, string folderPath, string filePrefix, CancellationToken ct = default)
    {
        return FileDownloader.DownloadIfExistsAsync(url, folderPath, filePrefix);
    }
}