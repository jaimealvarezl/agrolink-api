using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Lots.Commands.UpdatePaddock;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.UpdatePaddock;

[TestFixture]
public class UpdateLotPaddockCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdateLotPaddockCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private UpdateLotPaddockCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidMovement_UpdatesPaddockAndReturnsLotDto()
    {
        // Arrange
        const int farmId = 1;
        const int lotId = 10;
        const int oldPaddockId = 100;
        const int newPaddockId = 200;

        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = oldPaddockId,
            Paddock = new Paddock { Id = oldPaddockId, FarmId = farmId },
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
        };

        var newPaddock = new Paddock
        {
            Id = newPaddockId,
            Name = "New Paddock",
            FarmId = farmId,
        };

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(lotId))
            .ReturnsAsync(lot);

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(newPaddockId))
            .ReturnsAsync(newPaddock);

        var command = new UpdateLotPaddockCommand(farmId, lotId, newPaddockId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.PaddockId.ShouldBe(newPaddockId);
        result.PaddockName.ShouldBe(newPaddock.Name);

        lot.PaddockId.ShouldBe(newPaddockId);
        _mocker.GetMock<ILotRepository>().Verify(r => r.Update(lot), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_LotNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(It.IsAny<int>()))
            .ReturnsAsync((Lot?)null);

        var command = new UpdateLotPaddockCommand(1, 10, 200);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_LotFromDifferentFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        const int farmId = 1;
        const int otherFarmId = 2;
        var lot = new Lot
        {
            Id = 10,
            Paddock = new Paddock { FarmId = otherFarmId },
        };

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(10))
            .ReturnsAsync(lot);

        var command = new UpdateLotPaddockCommand(farmId, 10, 200);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NewPaddockNotFound_ThrowsNotFoundException()
    {
        // Arrange
        const int farmId = 1;
        var lot = new Lot
        {
            Id = 10,
            Paddock = new Paddock { FarmId = farmId },
        };

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(10))
            .ReturnsAsync(lot);

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync((Paddock?)null);

        var command = new UpdateLotPaddockCommand(farmId, 10, 200);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NewPaddockFromDifferentFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        const int farmId = 1;
        const int otherFarmId = 2;
        var lot = new Lot
        {
            Id = 10,
            Paddock = new Paddock { FarmId = farmId },
        };
        var otherPaddock = new Paddock { Id = 200, FarmId = otherFarmId };

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(10))
            .ReturnsAsync(lot);

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(200))
            .ReturnsAsync(otherPaddock);

        var command = new UpdateLotPaddockCommand(farmId, 10, 200);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
