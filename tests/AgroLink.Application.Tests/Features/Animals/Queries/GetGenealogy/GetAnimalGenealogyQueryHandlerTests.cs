using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetGenealogy;

[TestFixture]
public class GetAnimalGenealogyQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalGenealogyQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAnimalGenealogyQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimalWithGenealogy_ReturnsAnimalGenealogyDto()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        const int farmId = 10;
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
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetChildrenAsync(animalId))
            .ReturnsAsync(new List<Animal>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
    }

    [Test]
    public async Task Handle_AnimalFromAnotherFarm_ReturnsNull()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        const int currentFarmId = 10;
        const int animalFarmId = 20;
        var query = new GetAnimalGenealogyQuery(animalId, userId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = animalFarmId } },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync(animal);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 1;
        var query = new GetAnimalGenealogyQuery(animalId, userId);

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetAnimalDetailsAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
