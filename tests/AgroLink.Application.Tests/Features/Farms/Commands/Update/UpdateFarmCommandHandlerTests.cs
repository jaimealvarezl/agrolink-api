using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Update;

[TestFixture]
public class UpdateFarmCommandHandlerTests
{
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
    private UpdateFarmCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateFarmCommandHandler(_farmRepositoryMock.Object, _unitOfWorkMock.Object);
    }

    [Test]
    public async Task Handle_ValidUpdateFarmCommand_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var name = "Updated Farm";
        var location = "Updated Location";
        var command = new UpdateFarmCommand(farmId, name, location);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Farm",
            Location = "Old Location",
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(name);
        result.Location.ShouldBe(location);
        _farmRepositoryMock.Verify(r => r.Update(It.IsAny<Farm>()), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_FarmNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new UpdateFarmCommand(999, "Name", "Location");
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(999)).ReturnsAsync((Farm?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
