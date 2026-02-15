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
        var query = new GetAnimalColorsQuery("ne", 10);
        var expectedColors = new List<string> { "Negro", "Pinto Negro" };

        _animalRepositoryMock
            .Setup(r => r.GetDistinctColorsAsync("ne", 10))
            .ReturnsAsync(expectedColors);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count.ShouldBe(2);
        result.ShouldContain("Negro");
        result.ShouldContain("Pinto Negro");
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoMatch()
    {
        // Arrange
        var query = new GetAnimalColorsQuery("xyz", 10);
        _animalRepositoryMock
            .Setup(r => r.GetDistinctColorsAsync("xyz", 10))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
