namespace AgroLink.Application.Interfaces;

public interface IAwsS3Service
{
    Task UploadFileAsync(string key, Stream fileStream, string contentType);
    Task DeleteFileAsync(string key);
    string GetFileUrl(string key);
}
