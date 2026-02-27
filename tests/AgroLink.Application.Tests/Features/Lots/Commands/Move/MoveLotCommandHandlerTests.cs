using AgroLink.Application.Features.Lots.Commands.Move;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Move;

[TestFixture]
public class MoveLotCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<MoveLotCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private MoveLotCommandHandler _handler = null!;

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

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker.GetMock<ILotRepository>().Setup(r => r.Update(lot));
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.AddMovementAsync(It.IsAny<Movement>()))
            .Returns(Task.CompletedTask);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(toPaddockId))
            .ReturnsAsync(paddockTo);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(lotId);
        result.PaddockId.ShouldBe(toPaddockId);
        result.PaddockName.ShouldBe(paddockTo.Name);
        _mocker.GetMock<ILotRepository>().Verify(r => r.Update(lot), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
        _mocker
            .GetMock<IMovementRepository>()
            .Verify(
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
        _mocker
            .GetMock<IMovementRepository>()
            .Verify(r => r.AddMovementAsync(It.IsAny<Movement>()), Times.Never);
    }
}
