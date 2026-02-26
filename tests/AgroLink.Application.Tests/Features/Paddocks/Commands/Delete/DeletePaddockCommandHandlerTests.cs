using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Paddocks.Commands.Delete;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Delete;

[TestFixture]
public class DeletePaddockCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeletePaddockCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeletePaddockCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingPaddock_DeletesPaddock()
    {
        // Arrange
        var paddockId = 1;
        var farmId = 10;
        var command = new DeletePaddockCommand(paddockId);
        var paddock = new Paddock { Id = paddockId, FarmId = farmId };

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker.GetMock<IPaddockRepository>().Setup(r => r.Remove(paddock));
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<IPaddockRepository>().Verify(r => r.GetByIdAsync(paddockId), Times.Once);
        _mocker.GetMock<IPaddockRepository>().Verify(r => r.Remove(paddock), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
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

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

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

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddockId))
            .ReturnsAsync((Paddock?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Paddock not found");
        _mocker
            .GetMock<IPaddockRepository>()
            .Verify(r => r.Remove(It.IsAny<Paddock>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
