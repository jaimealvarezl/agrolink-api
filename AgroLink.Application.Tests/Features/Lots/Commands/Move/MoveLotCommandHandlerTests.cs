using AgroLink.Application.Features.Lots.Commands.Move;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Move;

[TestFixture]
public class MoveLotCommandHandlerTests
{
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IMovementRepository> _movementRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private MoveLotCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _movementRepositoryMock = new Mock<IMovementRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new MoveLotCommandHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object,
            _movementRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidMoveLotCommand_ReturnsLotDto()
    {
        // Arrange
        var lotId = 1;
        var toPaddockId = 2;
        var userId = 1;
        var command = new MoveLotCommand(lotId, toPaddockId, "Test Reason", userId);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = 1,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
        };
        var paddockTo = new Paddock { Id = toPaddockId, Name = "Paddock To" };

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _lotRepositoryMock.Setup(r => r.Update(lot));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _movementRepositoryMock
            .Setup(r => r.AddMovementAsync(It.IsAny<Movement>()))
            .Returns(Task.CompletedTask);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(toPaddockId)).ReturnsAsync(paddockTo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(lotId);
        result.PaddockId.ShouldBe(toPaddockId);
        result.PaddockName.ShouldBe(paddockTo.Name);
        _lotRepositoryMock.Verify(r => r.Update(lot), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
        _movementRepositoryMock.Verify(
            r =>
                r.AddMovementAsync(
                    It.Is<Movement>(m =>
                        m.EntityId == lotId
                        && m.FromId == 1
                        && m.ToId == toPaddockId
                        && m.UserId == userId
                    )
                ),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_NonExistingLot_ThrowsArgumentException()
    {
        // Arrange
        var lotId = 999;
        var toPaddockId = 2;
        var userId = 1;
        var command = new MoveLotCommand(lotId, toPaddockId, "Test Reason", userId);

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync((Lot?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Lot not found");
        _lotRepositoryMock.Verify(r => r.Update(It.IsAny<Lot>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
        _movementRepositoryMock.Verify(r => r.AddMovementAsync(It.IsAny<Movement>()), Times.Never);
    }
}