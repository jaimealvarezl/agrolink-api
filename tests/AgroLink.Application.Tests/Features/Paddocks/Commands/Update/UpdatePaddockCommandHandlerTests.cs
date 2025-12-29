using AgroLink.Application.Features.Paddocks.Commands.Update;
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
        var name = "Updated Paddock";
        var farmId = 2;
        var command = new UpdatePaddockCommand(paddockId, name, farmId);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Old Paddock",
            FarmId = 1,
        };
        var farm = new Farm { Id = farmId, Name = "Test Farm" };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(paddockId);
        result.Name.ShouldBe(name);
        result.FarmId.ShouldBe(farmId);
        _paddockRepositoryMock.Verify(r => r.Update(It.IsAny<Paddock>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_PaddockNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new UpdatePaddockCommand(999, "Name", 1);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Paddock?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
