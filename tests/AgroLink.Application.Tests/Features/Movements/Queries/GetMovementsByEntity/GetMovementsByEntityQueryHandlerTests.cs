using AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Movements.Queries.GetMovementsByEntity;

[TestFixture]
public class GetMovementsByEntityQueryHandlerTests
{
    private Mock<IMovementRepository> _movementRepositoryMock = null!;
    private GetMovementsByEntityQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _movementRepositoryMock = new Mock<IMovementRepository>();
        _handler = new GetMovementsByEntityQueryHandler(_movementRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ExistingEntityWithMovements_ReturnsMovementsDto()
    {
        // Arrange
        var entityType = "ANIMAL";
        var entityId = 1;
        var query = new GetMovementsByEntityQuery(entityType, entityId);
        var movements = new List<Movement>
        {
            new Movement
            {
                Id = 1,
                EntityType = entityType,
                EntityId = entityId,
                FromId = 10,
                ToId = 20,
                At = DateTime.UtcNow,
                UserId = 1,
            },
            new Movement
            {
                Id = 2,
                EntityType = entityType,
                EntityId = entityId,
                FromId = 20,
                ToId = 30,
                At = DateTime.UtcNow.AddHours(1),
                UserId = 1,
            },
        };
        var user = new User { Id = 1, Name = "Test User" };
        var animal = new Animal { Id = entityId, Tag = "TestAnimal" };
        var lotFrom = new Lot { Id = 10, Name = "Lot From" };
        var lotTo = new Lot { Id = 20, Name = "Lot To" };

        _movementRepositoryMock
            .Setup(r => r.GetMovementsByEntityAsync(entityType, entityId))
            .ReturnsAsync(movements);
        _movementRepositoryMock.Setup(r => r.GetUserByIdAsync(It.IsAny<int>())).ReturnsAsync(user);
        _movementRepositoryMock
            .Setup(r => r.GetAnimalByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(animal);
        _movementRepositoryMock.Setup(r => r.GetLotByIdAsync(10)).ReturnsAsync(lotFrom);
        _movementRepositoryMock.Setup(r => r.GetLotByIdAsync(20)).ReturnsAsync(lotTo);
        _movementRepositoryMock
            .Setup(r => r.GetLotByIdAsync(30))
            .ReturnsAsync(new Lot { Id = 30, Name = "Lot Final" });

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);

        // Latest movement (Movement 2)
        var first = result.First();
        first.EntityType.ShouldBe(entityType);
        first.EntityName.ShouldBe(animal.Tag);
        first.FromName.ShouldBe(lotTo.Name); // From Lot To (20)
        first.ToName.ShouldBe("Lot Final"); // To Lot Final (30)
        first.UserName.ShouldBe(user.Name);

        // Oldest movement (Movement 1)
        var last = result.Last();
        last.FromName.ShouldBe(lotFrom.Name); // From Lot From (10)
        last.ToName.ShouldBe(lotTo.Name); // To Lot To (20)
    }

    [Test]
    public async Task Handle_ExistingEntityWithNoMovements_ReturnsEmptyList()
    {
        // Arrange
        var entityType = "ANIMAL";
        var entityId = 1;
        var query = new GetMovementsByEntityQuery(entityType, entityId);

        _movementRepositoryMock
            .Setup(r => r.GetMovementsByEntityAsync(entityType, entityId))
            .ReturnsAsync(new List<Movement>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
