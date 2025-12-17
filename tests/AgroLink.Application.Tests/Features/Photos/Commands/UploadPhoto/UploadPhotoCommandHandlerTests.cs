using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Photos.Commands.UploadPhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Photos.Commands.UploadPhoto;

[TestFixture]
public class UploadPhotoCommandHandlerTests
{
    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<IAwsS3Service> _awsS3ServiceMock = null!;
    private UploadPhotoCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _awsS3ServiceMock = new Mock<IAwsS3Service>();
        _handler = new UploadPhotoCommandHandler(
            _photoRepositoryMock.Object,
            _awsS3ServiceMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidUploadPhotoCommand_ReturnsPhotoDto()
    {
        // Arrange
        var createPhotoDto = new CreatePhotoDto
        {
            EntityType = "ANIMAL",
            EntityId = 1,
            Description = "Test Photo",
        };
        var fileName = "test.jpg";
        var fileStream = new MemoryStream();
        var command = new UploadPhotoCommand(createPhotoDto, fileStream, fileName);
        var photo = new Photo
        {
            Id = 1,
            EntityType = "ANIMAL",
            EntityId = 1,
            UriLocal = $"local/animal/1/{fileName}",
        };

        _photoRepositoryMock
            .Setup(r => r.AddPhotoAsync(It.IsAny<Photo>()))
            .Callback<Photo>(p => p.Id = photo.Id)
            .Returns(Task.CompletedTask);
        _photoRepositoryMock
            .Setup(r => r.UpdatePhotoAsync(It.IsAny<Photo>()))
            .Returns(Task.CompletedTask);
        _awsS3ServiceMock
            .Setup(s =>
                s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>())
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(photo.Id);
        result.EntityType.ShouldBe(createPhotoDto.EntityType);
        result.UriRemote.ShouldNotBeNullOrEmpty();
        result.Uploaded.ShouldBeTrue();
        _photoRepositoryMock.Verify(r => r.AddPhotoAsync(It.IsAny<Photo>()), Times.Once);
        _awsS3ServiceMock.Verify(
            s => s.UploadFileAsync(It.IsAny<string>(), fileStream, It.IsAny<string>()),
            Times.Once
        );
        _photoRepositoryMock.Verify(
            r => r.UpdatePhotoAsync(It.Is<Photo>(p => p.Uploaded == true)),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_S3UploadFails_PhotoStillAddedToDbAsNotUploaded()
    {
        // Arrange
        var createPhotoDto = new CreatePhotoDto
        {
            EntityType = "ANIMAL",
            EntityId = 1,
            Description = "Test Photo",
        };
        var fileName = "test.jpg";
        var fileStream = new MemoryStream();
        var command = new UploadPhotoCommand(createPhotoDto, fileStream, fileName);
        var photo = new Photo
        {
            Id = 1,
            EntityType = "ANIMAL",
            EntityId = 1,
            UriLocal = $"local/animal/1/{fileName}",
        };

        _photoRepositoryMock
            .Setup(r => r.AddPhotoAsync(It.IsAny<Photo>()))
            .Callback<Photo>(p => p.Id = photo.Id)
            .Returns(Task.CompletedTask);
        _photoRepositoryMock
            .Setup(r => r.UpdatePhotoAsync(It.IsAny<Photo>()))
            .Returns(Task.CompletedTask);
        _awsS3ServiceMock
            .Setup(s =>
                s.UploadFileAsync(It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<string>())
            )
            .ThrowsAsync(new Exception("S3 upload error"));

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(photo.Id);
        result.UriRemote.ShouldBeNull(); // Should be null because S3 upload failed
        result.Uploaded.ShouldBeFalse(); // Should be false because S3 upload failed
        _photoRepositoryMock.Verify(r => r.AddPhotoAsync(It.IsAny<Photo>()), Times.Once);
        _awsS3ServiceMock.Verify(
            s => s.UploadFileAsync(It.IsAny<string>(), fileStream, It.IsAny<string>()),
            Times.Once
        );
        _photoRepositoryMock.Verify(r => r.UpdatePhotoAsync(It.IsAny<Photo>()), Times.Never); // No update if S3 fails
    }
}
