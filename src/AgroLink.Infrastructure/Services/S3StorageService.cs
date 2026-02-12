using AgroLink.Application.Interfaces;
using Amazon.S3;
using Amazon.S3.Model;
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
            "Uploading file to bucket {BucketName} with key {Key}, content type {ContentType} and size {Size}",
            _bucketName,
            key,
            contentType,
            contentLength
        );

        try
        {
            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false, // Let the caller manage the stream lifecycle
            };

            // Add metadata if useful
            putRequest.Metadata.Add("x-amz-meta-original-size", contentLength.ToString());

            await s3Client.PutObjectAsync(putRequest);

            logger.LogInformation("S3 upload successful via PutObjectAsync. Key: {Key}", key);
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
