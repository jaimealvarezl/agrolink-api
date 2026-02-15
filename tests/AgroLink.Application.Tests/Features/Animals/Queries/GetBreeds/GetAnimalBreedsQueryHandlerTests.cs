using AgroLink.Application.Features.Animals.Queries.GetBreeds;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetBreeds;

[TestFixture]
public class GetAnimalBreedsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _handler = new GetAnimalBreedsQueryHandler(_animalRepositoryMock.Object);
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private GetAnimalBreedsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsDistinctBreeds()
    {
        // Arrange
        const int userId = 1;
        var query = new GetAnimalBreedsQuery(userId);
        var expectedBreeds = new List<string> { "Angus", "Hereford", "Brahman" };

        _animalRepositoryMock
            .Setup(r => r.GetDistinctBreedsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedBreeds);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.ShouldContain("Angus");
        result.ShouldContain("Hereford");
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoAnimalsExist()
    {
        // Arrange
        const int userId = 1;
        var query = new GetAnimalBreedsQuery(userId);
        _animalRepositoryMock
            .Setup(r => r.GetDistinctBreedsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
