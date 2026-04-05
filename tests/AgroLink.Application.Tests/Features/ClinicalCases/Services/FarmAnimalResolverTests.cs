using AgroLink.Application.Common.Services;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.ClinicalCases.Services;

[TestFixture]
public class FarmAnimalResolverTests
{
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private FarmAnimalResolver _resolver = null!;

    [SetUp]
    public void SetUp()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _resolver = new FarmAnimalResolver(
            _farmRepositoryMock.Object,
            _animalRepositoryMock.Object
        );
    }

    [Test]
    public async Task ResolveAsync_WhenFarmReferenceIsMissing_ShouldReturnUnresolvedResult()
    {
        // Act
        var result = await _resolver.ResolveAsync(null, "Lola", "A-123");

        // Assert
        result.IsResolved.ShouldBeFalse();
        result.ResolutionMessage.ShouldContain("No pude identificar la granja");
    }

    [Test]
    public async Task ResolveAsync_WithValidFarmAndEarTag_ShouldReturnResolvedResult()
    {
        // Arrange
        var farm = new Farm { Id = 10, Name = "El Rosario" };
        var animal = new Animal
        {
            Id = 20,
            Name = "Lola",
            TagVisual = "A-123",
            LotId = 1,
        };

        _farmRepositoryMock
            .Setup(x => x.FindByReferenceAsync("El Rosario", It.IsAny<CancellationToken>()))
            .ReturnsAsync(farm);
        _animalRepositoryMock
            .Setup(x => x.GetByEarTagInFarmAsync(10, "A-123", It.IsAny<CancellationToken>()))
            .ReturnsAsync(animal);

        // Act
        var result = await _resolver.ResolveAsync("El Rosario", "Lola", "A-123");

        // Assert
        result.IsResolved.ShouldBeTrue();
        result.Farm!.Id.ShouldBe(10);
        result.Animal!.Id.ShouldBe(20);
    }
}
