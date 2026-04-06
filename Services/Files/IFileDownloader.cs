namespace Reports.Services.Files;

public interface IFileDownloader
{
    Task DownloadIfExistsAsync(string url, string folderPath, string filePrefix, CancellationToken ct = default);
}