using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Paddocks.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Delete;

[TestFixture]
public class DeletePaddockCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeletePaddockCommandHandler(
            _paddockRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private DeletePaddockCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingPaddock_DeletesPaddock()
    {
        // Arrange
        var paddockId = 1;
        var farmId = 10;
        var command = new DeletePaddockCommand(paddockId);
        var paddock = new Paddock { Id = paddockId, FarmId = farmId };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);
        _paddockRepositoryMock.Setup(r => r.Remove(paddock));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _paddockRepositoryMock.Verify(r => r.GetByIdAsync(paddockId), Times.Once);
        _paddockRepositoryMock.Verify(r => r.Remove(paddock), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_PaddockFromAnotherFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        var paddockId = 1;
        var currentFarmId = 10;
        var paddockFarmId = 20;
        var command = new DeletePaddockCommand(paddockId);
        var paddock = new Paddock { Id = paddockId, FarmId = paddockFarmId };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NonExistingPaddock_ThrowsArgumentException()
    {
        // Arrange
        var paddockId = 999;
        var command = new DeletePaddockCommand(paddockId);

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync((Paddock?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Paddock not found");
        _paddockRepositoryMock.Verify(r => r.Remove(It.IsAny<Paddock>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
