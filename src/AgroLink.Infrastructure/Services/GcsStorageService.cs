using System.Diagnostics;
using AgroLink.Application.Interfaces;
using Google.Cloud.Storage.V1;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class GcsStorageService(
    StorageClient storageClient,
    UrlSigner urlSigner,
    IConfiguration configuration,
    ILogger<GcsStorageService> logger
) : IStorageService
{
    private readonly string _bucketName = configuration["GCS:BucketName"] ?? "agrolink-files";

    public async Task UploadFileAsync(
        string key,
        Stream fileStream,
        string contentType,
        long contentLength
    )
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Uploading to GCS. Bucket: {Bucket}, Key: {Key}, ContentType: {ContentType}, Size: {Size}",
            _bucketName,
            key,
            contentType,
            contentLength
        );

        try
        {
            if (fileStream.CanSeek && fileStream.Position != 0)
            {
                fileStream.Position = 0;
            }

            await storageClient.UploadObjectAsync(_bucketName, key, contentType, fileStream);

            stopwatch.Stop();
            logger.LogInformation(
                "GCS upload successful. Key: {Key}, DurationMs: {Ms}",
                key,
                stopwatch.ElapsedMilliseconds
            );
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "GCS upload failed. Key: {Key}, DurationMs: {Ms}",
                key,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
    }

    public async Task DeleteFileAsync(string key)
    {
        try
        {
            await storageClient.DeleteObjectAsync(_bucketName, key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GCS delete failed. Key: {Key}", key);
            throw;
        }
    }

    public string GetFileUrl(string key)
    {
        return $"https://storage.googleapis.com/{_bucketName}/{key}";
    }

    public string GetPresignedUrl(string key, TimeSpan expiration)
    {
        try
        {
            return urlSigner.Sign(_bucketName, key, expiration, HttpMethod.Get);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "GCS pre-signed URL failed. Key: {Key}", key);
            return GetFileUrl(key);
        }
    }

    public async Task<byte[]?> GetFileBytesAsync(
        string key,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            using var ms = new MemoryStream();
            await storageClient.DownloadObjectAsync(
                _bucketName,
                key,
                ms,
                cancellationToken: cancellationToken
            );
            return ms.ToArray();
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "GCS download failed. Key: {Key}", key);
            return null;
        }
    }

    public string GetKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        var prefix = $"https://storage.googleapis.com/{_bucketName}/";
        return url.StartsWith(prefix)
            ? url[prefix.Length..]
            : new Uri(url).AbsolutePath.TrimStart('/');
    }
}
