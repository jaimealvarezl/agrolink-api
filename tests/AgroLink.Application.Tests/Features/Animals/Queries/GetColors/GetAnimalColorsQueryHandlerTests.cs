using AgroLink.Application.Features.Animals.Queries.GetColors;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetColors;

[TestFixture]
public class GetAnimalColorsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _handler = new GetAnimalColorsQueryHandler(_animalRepositoryMock.Object);
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private GetAnimalColorsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsDistinctColors()
    {
        // Arrange
        var userId = 1;
        var query = new GetAnimalColorsQuery(userId);
        var expectedColors = new List<string> { "Blanco", "Negro", "Pinto Negro" };

        _animalRepositoryMock
            .Setup(r => r.GetDistinctColorsAsync(userId))
            .ReturnsAsync(expectedColors);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(3);
        result.ShouldContain("Negro");
        result.ShouldContain("Blanco");
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoAnimalsExist()
    {
        // Arrange
        var userId = 1;
        var query = new GetAnimalColorsQuery(userId);
        _animalRepositoryMock
            .Setup(r => r.GetDistinctColorsAsync(userId))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
