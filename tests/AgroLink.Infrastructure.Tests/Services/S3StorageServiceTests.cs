using AgroLink.Infrastructure.Services;
using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Services;

[TestFixture]
public class S3StorageServiceTests
{
    [SetUp]
    public void Setup()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _loggerMock = new Mock<ILogger<S3StorageService>>();
    }

    private Mock<IAmazonS3> _s3ClientMock = null!;
    private Mock<ILogger<S3StorageService>> _loggerMock = null!;

    [Test]
    public void GetKeyFromUrl_StandardS3Url_ReturnsKey()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["AgroLink:S3BucketName"]).Returns("my-bucket");
        configurationMock.Setup(c => c["AWS:ServiceUrl"]).Returns("https://s3.amazonaws.com");

        var service = new S3StorageService(
            _s3ClientMock.Object,
            configurationMock.Object,
            _loggerMock.Object
        );
        var url = "https://my-bucket.s3.amazonaws.com/f/1/a/1/photo.png";

        // Act
        var key = service.GetKeyFromUrl(url);

        // Assert
        key.ShouldBe("f/1/a/1/photo.png");
    }

    [Test]
    public void GetKeyFromUrl_MinioLocalUrl_ReturnsKey()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["AgroLink:S3BucketName"]).Returns("my-bucket");
        configurationMock.Setup(c => c["AWS:ServiceUrl"]).Returns("http://localhost:9000");

        var service = new S3StorageService(
            _s3ClientMock.Object,
            configurationMock.Object,
            _loggerMock.Object
        );
        var url = "http://localhost:9000/my-bucket/f/1/a/1/photo.png";

        // Act
        var key = service.GetKeyFromUrl(url);

        // Assert
        key.ShouldBe("f/1/a/1/photo.png");
    }

    [Test]
    public void GetFileUrl_StandardS3_ReturnsCorrectFormat()
    {
        // Arrange
        var configurationMock = new Mock<IConfiguration>();
        configurationMock.Setup(c => c["AgroLink:S3BucketName"]).Returns("my-bucket");
        configurationMock.Setup(c => c["AWS:ServiceUrl"]).Returns("https://s3.amazonaws.com");

        var service = new S3StorageService(
            _s3ClientMock.Object,
            configurationMock.Object,
            _loggerMock.Object
        );
        var key = "f/1/a/1/photo.png";

        // Act
        var url = service.GetFileUrl(key);

        // Assert
        url.ShouldBe("https://my-bucket.s3.amazonaws.com/f/1/a/1/photo.png");
    }
}
