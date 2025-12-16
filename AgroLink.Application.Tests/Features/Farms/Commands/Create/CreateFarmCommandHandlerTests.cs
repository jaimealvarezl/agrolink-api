using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Farms.Commands.Create;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Create;

[TestFixture]
public class CreateFarmCommandHandlerTests
{
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private CreateFarmCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _handler = new CreateFarmCommandHandler(_farmRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ValidCreateFarmCommand_ReturnsFarmDto()
    {
        // Arrange
        var createFarmDto = new CreateFarmDto { Name = "Test Farm", Location = "Test Location" };
        var command = new CreateFarmCommand(createFarmDto);
        var farm = new Farm
        {
            Id = 1,
            Name = "Test Farm",
            Location = "Test Location",
        };

        _farmRepositoryMock
            .Setup(r => r.AddAsync(It.IsAny<Farm>()))
            .Callback<Farm>(f => f.Id = farm.Id); // Simulate DB ID generation
        _farmRepositoryMock.Setup(r => r.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farm.Id);
        result.Name.ShouldBe(farm.Name);
        _farmRepositoryMock.Verify(r => r.AddAsync(It.IsAny<Farm>()), Times.Once);
        _farmRepositoryMock.Verify(r => r.SaveChangesAsync(), Times.Once);
    }
}
