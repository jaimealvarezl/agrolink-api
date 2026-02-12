using System.Text;
using AgroLink.Application.Interfaces;
using Amazon.S3;
using Amazon.S3.Transfer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class S3StorageService(
    IAmazonS3 s3Client,
    IConfiguration configuration,
    ILogger<S3StorageService> logger
) : IStorageService
{
    private readonly string _bucketName =
        configuration["AgroLink:S3BucketName"]
        ?? configuration["AWS:S3BucketName"]
        ?? "agrolink-photos";

    private readonly string _serviceUrl =
        configuration["AWS:ServiceUrl"] ?? "https://s3.amazonaws.com";

    public async Task UploadFileAsync(
        string key,
        Stream fileStream,
        string contentType,
        long contentLength
    )
    {
        logger.LogInformation(
            "Uploading file to bucket {BucketName} with key {Key}, content type {ContentType} and expected size {Size}",
            _bucketName,
            key,
            contentType,
            contentLength
        );

        try
        {
            // Read into memory to ensure we have the full content and can diagnostic it
            using var ms = new MemoryStream();
            await fileStream.CopyToAsync(ms);
            ms.Position = 0;

            var actualSize = ms.Length;
            logger.LogInformation("Actual bytes read from stream: {ActualSize}", actualSize);

            if (actualSize == 0)
            {
                throw new InvalidOperationException("Source stream is empty.");
            }

            // Diagnostic: Log the first 32 bytes to check for Base64 or corruption
            var diagBuffer = new byte[32];
            var read = await ms.ReadAsync(diagBuffer, 0, 32);
            ms.Position = 0;
            logger.LogInformation(
                "First 32 bytes (Hex): {Hex}",
                BitConverter.ToString(diagBuffer, 0, read)
            );
            logger.LogInformation(
                "First 32 bytes (ASCII): {Text}",
                Encoding.ASCII.GetString(diagBuffer, 0, read)
            );

            // Use TransferUtility for more robust uploads
            var fileTransferUtility = new TransferUtility(s3Client);
            var uploadRequest = new TransferUtilityUploadRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = ms,
                ContentType = contentType,
            };

            await fileTransferUtility.UploadAsync(uploadRequest);

            logger.LogInformation("S3 upload successful via TransferUtility. Key: {Key}", key);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to upload file to S3 with key {Key} to bucket {Bucket}",
                key,
                _bucketName
            );
            throw;
        }
    }

    public async Task DeleteFileAsync(string key)
    {
        try
        {
            await s3Client.DeleteObjectAsync(_bucketName, key);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete file from S3 with key {Key}", key);
            throw;
        }
    }

    public string GetFileUrl(string key)
    {
        // If utilizing MinIO locally
        if (_serviceUrl.Contains("localhost") || _serviceUrl.Contains("minio"))
        {
            // For local development, we need to return the localhost URL
            // even if the internal service URL is 'http://minio:9000'
            var publicUrl = _serviceUrl.Replace("minio", "localhost");
            return $"{publicUrl}/{_bucketName}/{key}";
        }

        // Standard AWS S3 URL format
        return $"https://{_bucketName}.s3.amazonaws.com/{key}";
    }

    public string GetKeyFromUrl(string url)
    {
        if (string.IsNullOrEmpty(url))
        {
            return string.Empty;
        }

        var uri = new Uri(url);

        // If utilizing MinIO locally (Path Style): http://localhost:9000/bucket/key
        if (_serviceUrl.Contains("localhost") || _serviceUrl.Contains("minio"))
        {
            var parts = uri.AbsolutePath.TrimStart('/').Split('/', 2);
            return parts.Length > 1 ? parts[1] : parts[0];
        }

        // Standard AWS S3 URL format (Virtual Host Style): https://bucket.s3.amazonaws.com/key
        return uri.AbsolutePath.TrimStart('/');
    }
}
