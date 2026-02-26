using AgroLink.Application.Features.Animals.Queries.GetBreeds;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetBreeds;

[TestFixture]
public class GetAnimalBreedsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalBreedsQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAnimalBreedsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsDistinctBreeds()
    {
        // Arrange
        const int farmId = 1;
        var query = new GetAnimalBreedsQuery(farmId);
        var expectedBreeds = new List<string> { "Angus", "Hereford", "Brahman" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetDistinctBreedsAsync(farmId, It.IsAny<CancellationToken>()))
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
        const int farmId = 1;
        var query = new GetAnimalBreedsQuery(farmId);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetDistinctBreedsAsync(farmId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<string>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
