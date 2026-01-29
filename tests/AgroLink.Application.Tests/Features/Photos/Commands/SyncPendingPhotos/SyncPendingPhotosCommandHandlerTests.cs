using AgroLink.Application.Features.Photos.Commands.SyncPendingPhotos;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;

namespace AgroLink.Application.Tests.Features.Photos.Commands.SyncPendingPhotos;

[TestFixture]
public class SyncPendingPhotosCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _handler = new SyncPendingPhotosCommandHandler(_photoRepositoryMock.Object);
    }

    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private SyncPendingPhotosCommandHandler _handler = null!;

    [Test]
    public async Task Handle_PendingPhotosExist_UpdatesPhotos()
    {
        // Arrange
        var command = new SyncPendingPhotosCommand();
        var pendingPhotos = new List<Photo>
        {
            new()
            {
                Id = 1,
                Uploaded = false,
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Uploaded = false,
                CreatedAt = DateTime.UtcNow,
            },
        };

        _photoRepositoryMock.Setup(r => r.GetPendingPhotosAsync()).ReturnsAsync(pendingPhotos);
        _photoRepositoryMock
            .Setup(r => r.UpdatePhotoAsync(It.IsAny<Photo>()))
            .Returns(Task.CompletedTask);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _photoRepositoryMock.Verify(r => r.GetPendingPhotosAsync(), Times.Once);
        _photoRepositoryMock.Verify(
            r => r.UpdatePhotoAsync(It.Is<Photo>(p => p.Uploaded == false)),
            Times.Exactly(2)
        ); // Expect update for each pending photo
    }

    [Test]
    public async Task Handle_NoPendingPhotos_DoesNothing()
    {
        // Arrange
        var command = new SyncPendingPhotosCommand();

        _photoRepositoryMock.Setup(r => r.GetPendingPhotosAsync()).ReturnsAsync(new List<Photo>());

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _photoRepositoryMock.Verify(r => r.GetPendingPhotosAsync(), Times.Once);
        _photoRepositoryMock.Verify(r => r.UpdatePhotoAsync(It.IsAny<Photo>()), Times.Never);
    }
}
