namespace Reports.Services.Files;

public interface IFileDownloader
{
    Task DownloadIfExistsAsync(string url, string folderPath, string filePrefix, CancellationToken ct = default);
}
public sealed class FileDownloaderService : IFileDownloader
{
    public Task DownloadIfExistsAsync(string url, string folderPath, string filePrefix, CancellationToken ct = default)
    {
        return FileDownloader.DownloadIfExistsAsync(url, folderPath, filePrefix);
    }
}