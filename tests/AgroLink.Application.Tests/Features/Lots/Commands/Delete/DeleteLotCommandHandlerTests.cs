using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Lots.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Delete;

[TestFixture]
public class DeleteLotCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteLotCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeleteLotCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingLot_DeletesLot()
    {
        // Arrange
        const int lotId = 1;
        const int farmId = 10;
        var command = new DeleteLotCommand(lotId);
        var lot = new Lot { Id = lotId, PaddockId = 1 };
        var paddock = new Paddock { Id = 1, FarmId = farmId };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker.GetMock<IPaddockRepository>().Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker.GetMock<ILotRepository>().Setup(r => r.Remove(lot));
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<ILotRepository>().Verify(r => r.GetByIdAsync(lotId), Times.Once);
        _mocker.GetMock<ILotRepository>().Verify(r => r.Remove(lot), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_LotFromAnotherFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        const int lotId = 1;
        const int currentFarmId = 10;
        const int lotFarmId = 20;
        var command = new DeleteLotCommand(lotId);
        var lot = new Lot { Id = lotId, PaddockId = 1 };
        var paddock = new Paddock { Id = 1, FarmId = lotFarmId };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker.GetMock<IPaddockRepository>().Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NonExistingLot_ThrowsArgumentException()
    {
        // Arrange
        var lotId = 999;
        var command = new DeleteLotCommand(lotId);

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetByIdAsync(lotId))
            .ReturnsAsync((Lot?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Lot not found");
        _mocker.GetMock<ILotRepository>().Verify(r => r.Remove(It.IsAny<Lot>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
