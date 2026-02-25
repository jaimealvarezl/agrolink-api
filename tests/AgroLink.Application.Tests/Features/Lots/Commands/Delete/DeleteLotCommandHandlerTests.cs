using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Lots.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Commands.Delete;

[TestFixture]
public class DeleteLotCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new DeleteLotCommandHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
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

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);
        _lotRepositoryMock.Setup(r => r.Remove(lot));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _lotRepositoryMock.Verify(r => r.GetByIdAsync(lotId), Times.Once);
        _lotRepositoryMock.Verify(r => r.Remove(lot), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
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

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(currentFarmId);

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

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync((Lot?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Lot not found");
        _lotRepositoryMock.Verify(r => r.Remove(It.IsAny<Lot>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
