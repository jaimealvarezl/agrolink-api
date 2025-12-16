using AgroLink.Application.Features.Paddocks.Commands.Delete;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Delete;

[TestFixture]
public class DeletePaddockCommandHandlerTests
{
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private DeletePaddockCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new DeletePaddockCommandHandler(_paddockRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ExistingPaddock_DeletesPaddock()
    {
        // Arrange
        var paddockId = 1;
        var command = new DeletePaddockCommand(paddockId);
        var paddock = new Paddock { Id = paddockId };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _paddockRepositoryMock.Setup(r => r.Remove(paddock));
        _paddockRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _paddockRepositoryMock.Verify(r => r.GetByIdAsync(paddockId), Times.Once);
        _paddockRepositoryMock.Verify(r => r.Remove(paddock), Times.Once);
        _paddockRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
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
        _paddockRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
