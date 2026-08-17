namespace Reports.Services.Files;

public interface IFileDownloader
{
    Task DownloadIfExistsAsync(string url, string folderPath, string filePrefix, CancellationToken ct = default);
    Task<string?> MoveIfExistsAsync(string? sourceFilePath, string destinationFolderPath, CancellationToken ct = default);
    Task<string?> ExtractPdfPagesAsync(string? sourceFilePath, string destinationFolderPath, int pageCount,
        bool deleteOriginal = false, CancellationToken ct = default);

    Task<string?> ImageToPdfAsync(string? sourceFilePath, string destinationPath, bool deleteOriginal = false, CancellationToken ct = default);

    Task<string?> MergePdfsAsync(
        IEnumerable<string> sourceFilePaths,
        string destinationPath,
        bool deleteSources = false,
        CancellationToken ct = default);

    Task<string?> DownloadAsPdfAsync(
        string link,
        string prefix,
        string accountFolder,
        CancellationToken ct = default);
}
public sealed class FileDownloaderService : IFileDownloader
{
    public Task DownloadIfExistsAsync(string url, string folderPath, string filePrefix, CancellationToken ct = default)
    {
        return FileDownloader.DownloadIfExistsAsync(url, folderPath, filePrefix);
    }
    
    public Task<string?> MoveIfExistsAsync(string? sourceFilePath, string destinationFolderPath, CancellationToken ct = default)
    {
        return FileDownloader.MoveIfExistsAsync(sourceFilePath, destinationFolderPath, ct);
    }
    
    public Task<string?> ExtractPdfPagesAsync(string? sourceFilePath,
        string destinationFolderPath,
        int pageCount,
        bool deleteOriginal = false,
        CancellationToken ct = default)
    {
        return FileDownloader.ExtractPdfPagesAsync(sourceFilePath, destinationFolderPath, pageCount, deleteOriginal, ct);
    }

    public Task<string?> ImageToPdfAsync(
        string? sourceFilePath,
        string destinationPath,
        bool deleteOriginal = false,
        CancellationToken ct = default)
    {
        return FileDownloader.ImageToPdfAsync(sourceFilePath, destinationPath,deleteOriginal, ct);
    }

    public Task<string?> MergePdfsAsync(
        IEnumerable<string> sourceFilePaths,
        string destinationPath,
        bool deleteSources = false,
        CancellationToken ct = default)
    {
        return FileDownloader.MergePdfsAsync(sourceFilePaths, destinationPath, deleteSources, ct);
    }
    public Task<string?> DownloadAsPdfAsync(
        string link,
        string prefix,
        string accountFolder,
        CancellationToken ct = default)
    {
        return FileDownloader.DownloadAsPdfAsync(link, prefix, accountFolder, ct);
    }
}