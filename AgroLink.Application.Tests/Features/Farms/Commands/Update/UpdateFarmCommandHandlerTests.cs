using AgroLink.Application.DTOs;
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
    private UpdateFarmCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _handler = new UpdateFarmCommandHandler(_farmRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ValidUpdateFarmCommand_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var updateFarmDto = new UpdateFarmDto
        {
            Name = "Updated Name",
            Location = "Updated Location",
        };
        var command = new UpdateFarmCommand(farmId, updateFarmDto);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            Location = "Old Location",
            CreatedAt = DateTime.UtcNow,
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmRepositoryMock.Setup(r => r.Update(farm));
        _farmRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(updateFarmDto.Name);
        result.Location.ShouldBe(updateFarmDto.Location);
        _farmRepositoryMock.Verify(r => r.Update(farm), Times.Once);
        _farmRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NonExistingFarm_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 999;
        var updateFarmDto = new UpdateFarmDto { Name = "Updated Name" };
        var command = new UpdateFarmCommand(farmId, updateFarmDto);

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync((Farm?)null);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("Farm not found");
        _farmRepositoryMock.Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _farmRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Never);
    }
}
