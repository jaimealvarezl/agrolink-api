using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Movements.Commands.CreateMovement;
using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
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
    public async Task Handle_ValidBatchMovement_ReturnsMultipleMovementDtos()
    {
        var farmId = 10;
        var userId = 1;
        var command = new CreateMovementCommand(
            new CreateMovementDto
            {
                AnimalIds = new List<int> { 1, 2 },
                ToLotId = 20,
                Date = DateTime.UtcNow,
                Reason = "Test Move",
            },
            userId
        );

        var animal1 = new Animal
        {
            Id = 1,
            LotId = 10,
            TagVisual = "V001",
        };
        var animal2 = new Animal
        {
            Id = 2,
            LotId = 15,
            TagVisual = "V002",
        };

        var toLot = new Lot
        {
            Id = 20,
            Name = "To Lot",
            Paddock = new Paddock { FarmId = farmId },
        };
        var lot1 = new Lot
        {
            Id = 10,
            Name = "Lot 1",
            Paddock = new Paddock { FarmId = farmId },
        };
        var lot2 = new Lot
        {
            Id = 15,
            Name = "Lot 2",
            Paddock = new Paddock { FarmId = farmId },
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(20))
            .ReturnsAsync(toLot);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(10))
            .ReturnsAsync(lot1);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(15))
            .ReturnsAsync(lot2);

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdAsync(1, userId))
            .ReturnsAsync(animal1);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdAsync(2, userId))
            .ReturnsAsync(animal2);

        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetAnimalByIdAsync(1))
            .ReturnsAsync(animal1);
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetAnimalByIdAsync(2))
            .ReturnsAsync(animal2);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);

        var firstMovement = result.First();
        firstMovement.EntityId.ShouldBe(1);
        firstMovement.FromId.ShouldBe(10);
        firstMovement.ToId.ShouldBe(20);

        _mocker.GetMock<IUnitOfWork>().Verify(u => u.BeginTransactionAsync(), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.CommitTransactionAsync(), Times.Once);

        _mocker.GetMock<IAnimalRepository>().Verify(r => r.Update(animal1), Times.Once);
        _mocker.GetMock<IAnimalRepository>().Verify(r => r.Update(animal2), Times.Once);

        _mocker
            .GetMock<IMovementRepository>()
            .Verify(r => r.AddMovementAsync(It.IsAny<Movement>()), Times.Exactly(2));
    }

    [Test]
    public async Task Handle_EmptyAnimalIds_ThrowsArgumentException()
    {
        var command = new CreateMovementCommand(
            new CreateMovementDto { AnimalIds = new List<int>(), ToLotId = 20 },
            1
        );
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(10);

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_InvalidToLotId_ThrowsArgumentException()
    {
        var command = new CreateMovementCommand(
            new CreateMovementDto
            {
                AnimalIds = new List<int> { 1 },
                ToLotId = 20,
            },
            1
        );

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(10);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(20))
            .ReturnsAsync((Lot?)null);

        await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_AnimalBelongsToDifferentFarm_ThrowsForbiddenAccessException()
    {
        var farmId = 10;
        var differentFarmId = 99;

        var command = new CreateMovementCommand(
            new CreateMovementDto
            {
                AnimalIds = new List<int> { 1 },
                ToLotId = 20,
            },
            1
        );

        var toLot = new Lot
        {
            Id = 20,
            Paddock = new Paddock { FarmId = farmId },
        };
        var animal1 = new Animal { Id = 1, LotId = 10 };
        var differentFarmLot = new Lot
        {
            Id = 10,
            Paddock = new Paddock { FarmId = differentFarmId },
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(20))
            .ReturnsAsync(toLot);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(10))
            .ReturnsAsync(differentFarmLot);
        _mocker.GetMock<IAnimalRepository>().Setup(r => r.GetByIdAsync(1, 1)).ReturnsAsync(animal1);

        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );

        _mocker.GetMock<IUnitOfWork>().Verify(u => u.RollbackTransactionAsync(), Times.Once);
    }
}
