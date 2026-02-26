using AgroLink.Application.Features.Movements.Commands.CreateMovement;
using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Movements.Commands.CreateMovement;

[TestFixture]
public class CreateMovementCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreateMovementCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreateMovementCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCreateMovementCommand_ReturnsMovementDto()
    {
        // Arrange
        var createMovementDto = new CreateMovementDto
        {
            EntityType = "ANIMAL",
            EntityId = 1,
            FromId = 10,
            ToId = 20,
            At = DateTime.UtcNow,
            Reason = "Test Move",
        };
        var userId = 1;
        var command = new CreateMovementCommand(createMovementDto, userId);
        var movement = new Movement
        {
            Id = 1,
            EntityType = "ANIMAL",
            EntityId = 1,
            FromId = 10,
            ToId = 20,
            At = DateTime.UtcNow,
            Reason = "Test Move",
            UserId = userId,
        };
        var user = new User { Id = userId, Name = "Test User" };
        var animal = new Animal
        {
            Id = 1,
            TagVisual = "V001",
            Cuia = "CUIA-1",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
        };
        var lotFrom = new Lot { Id = 10, Name = "Lot From" };
        var lotTo = new Lot { Id = 20, Name = "Lot To" };

        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.AddMovementAsync(It.IsAny<Movement>()))
            .Callback<Movement>(m => m.Id = movement.Id); // Simulate DB ID generation
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetUserByIdAsync(userId))
            .ReturnsAsync(user);
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetAnimalByIdAsync(animal.Id))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetLotByIdAsync(lotFrom.Id))
            .ReturnsAsync(lotFrom);
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetLotByIdAsync(lotTo.Id))
            .ReturnsAsync(lotTo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(movement.Id);
        result.EntityType.ShouldBe(createMovementDto.EntityType);
        result.EntityName.ShouldBe(animal.TagVisual);
        result.FromName.ShouldBe(lotFrom.Name);
        result.ToName.ShouldBe(lotTo.Name);
        result.UserName.ShouldBe(user.Name);
        _mocker
            .GetMock<IMovementRepository>()
            .Verify(r => r.AddMovementAsync(It.IsAny<Movement>()), Times.Once);
    }
}
