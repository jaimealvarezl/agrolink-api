using System.Linq.Expressions;
using AgroLink.Application.Features.Movements.Queries.GetMovementsByEntity;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Movements.Queries.GetMovementsByEntity;

[TestFixture]
public class GetMovementsByEntityQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetMovementsByEntityQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetMovementsByEntityQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingEntityWithMovements_ReturnsMovementsDto()
    {
        // Arrange
        var entityType = EntityTypes.Animal;
        var entityId = 1;
        var query = new GetMovementsByEntityQuery(entityType, entityId);
        var movements = new List<Movement>
        {
            new()
            {
                Id = 1,
                EntityType = entityType,
                EntityId = entityId,
                FromId = 10,
                ToId = 20,
                At = DateTime.UtcNow,
                UserId = 1,
            },
            new()
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
        var animal = new Animal
        {
            Id = 1,
            TagVisual = "V001",
            Cuia = "CUIA-Test",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
        };
        var lotFrom = new Lot { Id = 10, Name = "Lot From" };
        var lotTo = new Lot { Id = 20, Name = "Lot To" };
        var lotFinal = new Lot { Id = 30, Name = "Lot Final" };

        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetMovementsByEntityAsync(entityType, entityId))
            .ReturnsAsync(movements);

        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Animal, bool>>>()))
            .ReturnsAsync(new List<Animal> { animal });

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
        first.EntityType.ShouldBe(entityType);
        first.EntityName.ShouldBe(animal.TagVisual);
        first.FromName.ShouldBe(lotTo.Name); // From Lot To (20)
        first.ToName.ShouldBe(lotFinal.Name); // To Lot Final (30)
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
        var entityType = EntityTypes.Animal;
        var entityId = 1;
        var query = new GetMovementsByEntityQuery(entityType, entityId);

        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetMovementsByEntityAsync(entityType, entityId))
            .ReturnsAsync(new List<Movement>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
