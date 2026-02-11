using AgroLink.Application.Features.Photos.Commands.DeletePhoto;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Photos.Commands.DeletePhoto;

[TestFixture]
public class DeletePhotoCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _handler = new DeletePhotoCommandHandler(
            _photoRepositoryMock.Object,
            _storageServiceMock.Object
        );
    }

    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private DeletePhotoCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingPhoto_DeletesPhoto()
    {
        // Arrange
        var photoId = 1;
        var command = new DeletePhotoCommand(photoId);
        var photo = new Photo { Id = photoId, UriRemote = "http://s3.aws.com/key" };

        _photoRepositoryMock.Setup(r => r.GetPhotoByIdAsync(photoId)).ReturnsAsync(photo);
        _storageServiceMock
            .Setup(s => s.DeleteFileAsync(It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        _photoRepositoryMock.Setup(r => r.DeletePhotoAsync(photo)).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _photoRepositoryMock.Verify(r => r.GetPhotoByIdAsync(photoId), Times.Once);
        _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Once);
        _photoRepositoryMock.Verify(r => r.DeletePhotoAsync(photo), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingPhoto_ThrowsArgumentException()
    {
        // Arrange
        var photoId = 999;
        var command = new DeletePhotoCommand(photoId);

        _photoRepositoryMock.Setup(r => r.GetPhotoByIdAsync(photoId)).ReturnsAsync((Photo?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Photo not found");
        _photoRepositoryMock.Verify(r => r.DeletePhotoAsync(It.IsAny<Photo>()), Times.Never);
        _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never);
    }

    [Test]
    public async Task Handle_ExistingPhotoNoRemoteUri_DeletesPhotoWithoutS3Call()
    {
        // Arrange
        var photoId = 1;
        var command = new DeletePhotoCommand(photoId);
        var photo = new Photo { Id = photoId, UriRemote = null };

        _photoRepositoryMock.Setup(r => r.GetPhotoByIdAsync(photoId)).ReturnsAsync(photo);
        _photoRepositoryMock.Setup(r => r.DeletePhotoAsync(photo)).Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _photoRepositoryMock.Verify(r => r.GetPhotoByIdAsync(photoId), Times.Once);
        _storageServiceMock.Verify(s => s.DeleteFileAsync(It.IsAny<string>()), Times.Never); // Should not call S3
        _photoRepositoryMock.Verify(r => r.DeletePhotoAsync(photo), Times.Once);
    }
}
