namespace AgroLink.Application.Interfaces;

public interface IStorageService
{
    Task UploadFileAsync(
        string key,
        Stream fileStream,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default
    );

    Task DeleteFileAsync(string key, CancellationToken cancellationToken = default);
    string GetFileUrl(string key);
    string GetPresignedUrl(string key, TimeSpan expiration);
    string GetKeyFromUrl(string url);
    Task<byte[]?> GetFileBytesAsync(string key, CancellationToken cancellationToken = default);
}
