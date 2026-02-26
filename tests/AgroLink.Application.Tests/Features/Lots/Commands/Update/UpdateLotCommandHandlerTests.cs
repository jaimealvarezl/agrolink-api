using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Lots.Commands.Update;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Update;

[TestFixture]
public class UpdateLotCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdateLotCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private UpdateLotCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdateLotCommand_ReturnsLotDto()
    {
        // Arrange
        const int lotId = 1;
        const int farmId = 10;
        var updateLotDto = new UpdateLotDto
        {
            Name = "Updated Lot Name",
            PaddockId = 2,
            Status = "INACTIVE",
        };
        var command = new UpdateLotCommand(lotId, updateLotDto);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Old Lot Name",
            PaddockId = 1,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
        };
        var oldPaddock = new Paddock { Id = 1, FarmId = farmId };
        var newPaddock = new Paddock
        {
            Id = 2,
            Name = "New Paddock",
            FarmId = farmId,
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker.GetMock<ILotRepository>().Setup(r => r.Update(lot));
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(1))
            .ReturnsAsync(oldPaddock);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(newPaddock.Id))
            .ReturnsAsync(newPaddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(lotId);
        result.Name.ShouldBe(updateLotDto.Name);
        result.PaddockId.ShouldBe(updateLotDto.PaddockId.Value);
        result.Status.ShouldBe(updateLotDto.Status);
        result.PaddockName.ShouldBe(newPaddock.Name);
        _mocker.GetMock<ILotRepository>().Verify(r => r.Update(lot), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_LotFromAnotherFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        const int lotId = 1;
        const int currentFarmId = 10;
        const int lotFarmId = 20;
        var updateLotDto = new UpdateLotDto { Name = "Updated Name" };
        var command = new UpdateLotCommand(lotId, updateLotDto);
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
        const int lotId = 999;
        var updateLotDto = new UpdateLotDto { Name = "Updated Name" };
        var command = new UpdateLotCommand(lotId, updateLotDto);

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetByIdAsync(lotId))
            .ReturnsAsync((Lot?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Lot not found");
        _mocker.GetMock<ILotRepository>().Verify(r => r.Update(It.IsAny<Lot>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
