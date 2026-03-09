using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Movements.Queries.GetMovementsByAnimal;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Movements.Queries.GetMovementsByAnimal;

[TestFixture]
public class GetMovementsByAnimalQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetMovementsByAnimalQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetMovementsByAnimalQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingAnimalWithMovements_ReturnsMovementsDto()
    {
        // Arrange
        var farmId = 1;
        var animalId = 1;
        var query = new GetMovementsByAnimalQuery(farmId, animalId);
        var movements = new List<Movement>
        {
            new()
            {
                Id = 1,
                AnimalId = animalId,
                FromLotId = 10,
                ToLotId = 20,
                At = DateTime.UtcNow,
                UserId = 1,
            },
            new()
            {
                Id = 2,
                AnimalId = animalId,
                FromLotId = 20,
                ToLotId = 30,
                At = DateTime.UtcNow.AddHours(1),
                UserId = 1,
            },
        };
        var user = new User { Id = 1, Name = "Test User" };
        var animal = new Animal
        {
            Id = 1,
            TagVisual = "V001",
            Cuia = "CUIA-Test",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };
        var lotFrom = new Lot { Id = 10, Name = "Lot From" };
        var lotTo = new Lot { Id = 20, Name = "Lot To" };
        var lotFinal = new Lot { Id = 30, Name = "Lot Final" };

        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetMovementsByAnimalAsync(animalId))
            .ReturnsAsync(movements);

        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(animalId))
            .ReturnsAsync(animal);

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>()))
            .ReturnsAsync(new List<Lot> { lotFrom, lotTo, lotFinal });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);

        // Latest movement (Movement 2) -> first in the result because of OrderByDescending
        var first = result.First();
        first.AnimalId.ShouldBe(animalId);
        first.AnimalName.ShouldBe(animal.TagVisual);
        first.FromLotName.ShouldBe(lotTo.Name); // From Lot To (20)
        first.ToLotName.ShouldBe(lotFinal.Name); // To Lot Final (30)
        first.UserName.ShouldBe(user.Name);

        // Oldest movement (Movement 1)
        var last = result.Last();
        last.FromLotName.ShouldBe(lotFrom.Name); // From Lot From (10)
        last.ToLotName.ShouldBe(lotTo.Name); // To Lot To (20)
    }

    [Test]
    public async Task Handle_AnimalFromDifferentFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        var farmId = 1;
        var differentFarmId = 2;
        var animalId = 1;
        var query = new GetMovementsByAnimalQuery(farmId, animalId);

        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = differentFarmId } },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(animalId))
            .ReturnsAsync(animal);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(query, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_AnimalNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var farmId = 1;
        var animalId = 1;
        var query = new GetMovementsByAnimalQuery(farmId, animalId);

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(animalId))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() => _handler.Handle(query, CancellationToken.None));
    }

    [Test]
    public async Task Handle_ExistingAnimalWithNoMovements_ReturnsEmptyList()
    {
        // Arrange
        var farmId = 1;
        var animalId = 1;
        var query = new GetMovementsByAnimalQuery(farmId, animalId);
        var animal = new Animal
        {
            Id = animalId,
            Lot = new Lot { Paddock = new Paddock { FarmId = farmId } },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(animalId))
            .ReturnsAsync(animal);

        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetMovementsByAnimalAsync(animalId))
            .ReturnsAsync(new List<Movement>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
