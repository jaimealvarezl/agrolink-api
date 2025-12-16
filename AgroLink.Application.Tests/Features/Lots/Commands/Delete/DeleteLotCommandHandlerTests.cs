using AgroLink.Application.Features.Lots.Commands.Delete;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Delete;

[TestFixture]
public class DeleteLotCommandHandlerTests
{
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private DeleteLotCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _handler = new DeleteLotCommandHandler(_lotRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ExistingLot_DeletesLot()
    {
        // Arrange
        var lotId = 1;
        var command = new DeleteLotCommand(lotId);
        var lot = new Lot { Id = lotId };

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _lotRepositoryMock.Setup(r => r.Remove(lot));
        _lotRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _lotRepositoryMock.Verify(r => r.GetByIdAsync(lotId), Times.Once);
        _lotRepositoryMock.Verify(r => r.Remove(lot), Times.Once);
        _lotRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingLot_ThrowsArgumentException()
    {
        // Arrange
        var lotId = 999;
        var command = new DeleteLotCommand(lotId);

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync((Lot?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Lot not found");
        _lotRepositoryMock.Verify(r => r.Remove(It.IsAny<Lot>()), Times.Never);
        _lotRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
