using System.IO;
using System.Threading.Tasks;
using AgroLink.Application.Interfaces;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Configuration; // Added

namespace AgroLink.Infrastructure.Services;

public class AwsS3Service(IAmazonS3 s3Client, IConfiguration configuration) : IAwsS3Service
{
    private readonly string _bucketName = configuration["AWS:S3BucketName"] ?? "agrolink-photos";

    public async Task UploadFileAsync(string key, Stream fileStream, string contentType)
    {
        var request = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
        };

        await s3Client.PutObjectAsync(request);
    }

    public async Task DeleteFileAsync(string key)
    {
        await s3Client.DeleteObjectAsync(_bucketName, key);
    }
}
