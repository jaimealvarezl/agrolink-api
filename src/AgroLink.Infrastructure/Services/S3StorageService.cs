using System.Diagnostics;
using System.Globalization;
using System.Security.Cryptography;
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
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation(
            "Starting upload to S3. Bucket: {BucketName}, Key: {Key}, ContentType: {ContentType}, Size: {Size}",
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

            string? md5Digest = null;
            if (fileStream.CanSeek)
            {
                using var md5 = MD5.Create();
                // ComputeHash reads the stream to the end
                var hashBytes = await md5.ComputeHashAsync(fileStream);
                md5Digest = Convert.ToBase64String(hashBytes);

                // Reset position for the actual upload
                fileStream.Position = 0;
            }

            var putRequest = new PutObjectRequest
            {
                BucketName = _bucketName,
                Key = key,
                InputStream = fileStream,
                ContentType = contentType,
                AutoCloseStream = false, // Let the caller manage the stream lifecycle
                MD5Digest = md5Digest, // S3 will verify this hash matches the uploaded content
            };

            putRequest.Headers.ContentLength = contentLength;

            // Add metadata if useful
            putRequest.Metadata.Add(
                "x-amz-meta-original-size",
                contentLength.ToString(CultureInfo.InvariantCulture)
            );

            await s3Client.PutObjectAsync(putRequest);

            stopwatch.Stop();
            logger.LogInformation(
                "S3 upload successful. Key: {Key}, DurationMs: {DurationMs}, Size: {Size}",
                key,
                stopwatch.ElapsedMilliseconds,
                contentLength
            );
        }
        catch (AmazonS3Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "AWS S3 Error during upload. Key: {Key}, Bucket: {Bucket}, ErrorCode: {ErrorCode}, RequestId: {RequestId}, DurationMs: {DurationMs}",
                key,
                _bucketName,
                ex.ErrorCode,
                ex.RequestId,
                stopwatch.ElapsedMilliseconds
            );
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            logger.LogError(
                ex,
                "Unexpected error during S3 upload. Key: {Key}, Bucket: {Bucket}, DurationMs: {DurationMs}",
                key,
                _bucketName,
                stopwatch.ElapsedMilliseconds
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

    public string GetPresignedUrl(string key, TimeSpan expiration)
    {
        try
        {
            var request = new GetPreSignedUrlRequest
            {
                BucketName = _bucketName,
                Key = key,
                Expires = DateTime.UtcNow.Add(expiration),
            };

            return s3Client.GetPreSignedURL(request);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to generate pre-signed URL for key {Key}", key);
            return GetFileUrl(key); // Fallback to direct URL if signing fails
        }
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
