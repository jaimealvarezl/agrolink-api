namespace AgroLink.Application.Interfaces;

public interface IStorageService
{
    Task UploadFileAsync(string key, Stream fileStream, string contentType, long contentLength);
    Task DeleteFileAsync(string key);
    string GetFileUrl(string key);
    string GetPresignedUrl(string key, TimeSpan expiration);
    string GetKeyFromUrl(string url);
}
