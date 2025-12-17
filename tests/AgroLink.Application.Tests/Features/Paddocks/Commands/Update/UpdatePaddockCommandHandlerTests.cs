using AgroLink.Application.Features.Paddocks.Commands.Update;
using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Update;

[TestFixture]
public class UpdatePaddockCommandHandlerTests
{
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private UpdatePaddockCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdatePaddockCommandHandler(
            _paddockRepositoryMock.Object,
            _farmRepositoryMock.Object,
            _unitOfWorkMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidUpdatePaddockCommand_ReturnsPaddockDto()
    {
        // Arrange
        var paddockId = 1;
        var updatePaddockDto = new UpdatePaddockDto { Name = "Updated Paddock Name", FarmId = 2 };
        var command = new UpdatePaddockCommand(paddockId, updatePaddockDto);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Old Paddock Name",
            FarmId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        var newFarm = new Farm { Id = 2, Name = "New Farm" };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _paddockRepositoryMock.Setup(r => r.Update(paddock));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(newFarm.Id)).ReturnsAsync(newFarm);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(paddockId);
        result.Name.ShouldBe(updatePaddockDto.Name);
        result.FarmId.ShouldBe(updatePaddockDto.FarmId.Value);
        result.FarmName.ShouldBe(newFarm.Name);
        _paddockRepositoryMock.Verify(r => r.Update(paddock), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingPaddock_ThrowsArgumentException()
    {
        // Arrange
        var paddockId = 999;
        var updatePaddockDto = new UpdatePaddockDto { Name = "Updated Name" };
        var command = new UpdatePaddockCommand(paddockId, updatePaddockDto);

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync((Paddock?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Paddock not found");
        _paddockRepositoryMock.Verify(r => r.Update(It.IsAny<Paddock>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
