using AgroLink.Application.Features.Animals.Queries.GetColors;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetColors;

[TestFixture]
public class GetAnimalColorsQueryHandlerTests
{
    private AutoMocker _mocker = null!;
    private GetAnimalColorsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalColorsQueryHandler>();
    }

    [Test]
    public async Task Handle_ReturnsDistinctColors()
    {
        // Arrange
        var userId = 1;
        var query = new GetAnimalColorsQuery(userId);
        var expectedColors = new List<string> { "Blanco", "Negro", "Pinto Negro" };

        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.GetDistinctColorsAsync(userId, It.IsAny<CancellationToken>()))
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
        _mocker.GetMock<IAnimalRepository>()
            .Setup(r => r.GetDistinctColorsAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
