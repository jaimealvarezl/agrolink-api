using AgroLink.Application.Features.Photos.Queries.GetPhotosByEntity;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Photos.Queries.GetPhotosByEntity;

[TestFixture]
public class GetPhotosByEntityQueryHandlerTests
{
    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private GetPhotosByEntityQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _handler = new GetPhotosByEntityQueryHandler(_photoRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ExistingEntityWithPhotos_ReturnsPhotosDto()
    {
        // Arrange
        var entityType = "ANIMAL";
        var entityId = 1;
        var query = new GetPhotosByEntityQuery(entityType, entityId);
        var photos = new List<Photo>
        {
            new Photo
            {
                Id = 1,
                EntityType = entityType,
                EntityId = entityId,
                UriLocal = "local/1",
                CreatedAt = DateTime.UtcNow,
            },
            new Photo
            {
                Id = 2,
                EntityType = entityType,
                EntityId = entityId,
                UriLocal = "local/2",
                CreatedAt = DateTime.UtcNow,
            },
        };

        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(entityType, entityId))
            .ReturnsAsync(photos);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().EntityType.ShouldBe(entityType);
        result.First().EntityId.ShouldBe(entityId);
    }

    [Test]
    public async Task Handle_ExistingEntityWithNoPhotos_ReturnsEmptyList()
    {
        // Arrange
        var entityType = "ANIMAL";
        var entityId = 1;
        var query = new GetPhotosByEntityQuery(entityType, entityId);

        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(entityType, entityId))
            .ReturnsAsync(new List<Photo>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
