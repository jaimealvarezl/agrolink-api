using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Lots.Commands.Update;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Update;

[TestFixture]
public class UpdateLotCommandHandlerTests
{
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private UpdateLotCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new UpdateLotCommandHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidUpdateLotCommand_ReturnsLotDto()
    {
        // Arrange
        var lotId = 1;
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
        var newPaddock = new Paddock { Id = 2, Name = "New Paddock" };

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _lotRepositoryMock.Setup(r => r.Update(lot));
        _lotRepositoryMock.Setup(r => r.SaveChangesAsync()).Returns(Task.CompletedTask);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(newPaddock.Id)).ReturnsAsync(newPaddock);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(lotId);
        result.Name.ShouldBe(updateLotDto.Name);
        result.PaddockId.ShouldBe(updateLotDto.PaddockId.Value);
        result.Status.ShouldBe(updateLotDto.Status);
        result.PaddockName.ShouldBe(newPaddock.Name);
        _lotRepositoryMock.Verify(r => r.Update(lot), Times.Once);
        _lotRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingLot_ThrowsArgumentException()
    {
        // Arrange
        var lotId = 999;
        var updateLotDto = new UpdateLotDto { Name = "Updated Name" };
        var command = new UpdateLotCommand(lotId, updateLotDto);

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync((Lot?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Lot not found");
        _lotRepositoryMock.Verify(r => r.Update(It.IsAny<Lot>()), Times.Never);
        _lotRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
