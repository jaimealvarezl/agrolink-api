using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetGenealogy;

[TestFixture]
public class GetAnimalGenealogyQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _handler = new GetAnimalGenealogyQueryHandler(_animalRepositoryMock.Object);
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private GetAnimalGenealogyQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimalWithGenealogy_ReturnsAnimalGenealogyDto()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        var query = new GetAnimalGenealogyQuery(animalId, userId);

        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Animal",
            Sex = Sex.Male,
            BirthDate = DateTime.Now.AddYears(-2),
            LotId = 1,
            Lot = new Lot { Paddock = new Paddock { FarmId = 1 } },
        };

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _animalRepositoryMock
            .Setup(r => r.GetChildrenAsync(animalId))
            .ReturnsAsync(new List<Animal>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 1;
        var query = new GetAnimalGenealogyQuery(animalId, userId);

        _animalRepositoryMock
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
