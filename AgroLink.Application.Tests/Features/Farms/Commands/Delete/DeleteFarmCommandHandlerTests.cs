using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Delete;

[TestFixture]
public class DeleteFarmCommandHandlerTests
{
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private DeleteFarmCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new DeleteFarmCommandHandler(_farmRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Test]
    public async Task Handle_ExistingFarm_DeletesFarm()
    {
        // Arrange
        var farmId = 1;
        var command = new DeleteFarmCommand(farmId);
        var farm = new Farm { Id = farmId };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmRepositoryMock.Setup(r => r.Remove(farm));
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _farmRepositoryMock.Verify(r => r.GetByIdAsync(farmId), Times.Once);
        _farmRepositoryMock.Verify(r => r.Remove(farm), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingFarm_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 999;
        var command = new DeleteFarmCommand(farmId);

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync((Farm?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Farm not found");
        _farmRepositoryMock.Verify(r => r.Remove(It.IsAny<Farm>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}