using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetById;

[TestFixture]
public class GetAnimalByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalByIdQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAnimalByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimal_ReturnsAnimalDto()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        const int farmId = 100;
        var query = new GetAnimalByIdQuery(animalId, userId);
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Animal 1",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            CreatedAt = DateTime.UtcNow,
            LifeStatus = LifeStatus.Active,
            Lot = new Lot
            {
                Name = "Test Lot",
                Paddock = new Paddock { FarmId = farmId },
            },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync(animal);

        _mocker
            .GetMock<IAnimalOwnerRepository>()
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());

        _mocker
            .GetMock<IAnimalPhotoRepository>()
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalPhoto>());

        // Setup CurrentUserService to match FarmId
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.TagVisual.ShouldBe(animal.TagVisual);
        result.LotName.ShouldBe("Test Lot");
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 1;
        var query = new GetAnimalByIdQuery(animalId, userId);

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenFarmIdDoesNotMatch_ReturnsNull()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        const int animalFarmId = 100;
        const int userContextFarmId = 200; // Different Farm

        var query = new GetAnimalByIdQuery(animalId, userId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            Lot = new Lot
            {
                Name = "Test Lot",
                Paddock = new Paddock { FarmId = animalFarmId },
            },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync(animal);

        // Setup CurrentUserService to return a DIFFERENT FarmId
        _mocker
            .GetMock<ICurrentUserService>()
            .Setup(s => s.CurrentFarmId)
            .Returns(userContextFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull(); // Should filter out because of farm mismatch
    }
}
