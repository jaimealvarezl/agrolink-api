using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Paddocks.Commands.Update;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Update;

[TestFixture]
public class UpdatePaddockCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdatePaddockCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private UpdatePaddockCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdatePaddockCommand_ReturnsPaddockDto()
    {
        // Arrange
        var paddockId = 1;
        var name = "Updated Paddock";
        var farmId = 2;
        var currentFarmId = 1;
        var area = 20.0m;
        var areaType = "Manzana";
        var command = new UpdatePaddockCommand(paddockId, name, farmId, area, areaType);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Old Paddock",
            FarmId = currentFarmId,
        };
        var farm = new Farm { Id = farmId, Name = "Test Farm" };

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);
        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(paddockId);
        result.Name.ShouldBe(name);
        result.FarmId.ShouldBe(farmId);
        result.Area.ShouldBe(area);
        result.AreaType.ShouldBe(areaType);
        _mocker
            .GetMock<IPaddockRepository>()
            .Verify(r => r.Update(It.IsAny<Paddock>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_PaddockFromAnotherFarm_ThrowsForbiddenAccessException()
    {
        // Arrange
        var paddockId = 1;
        var currentFarmId = 10;
        var paddockFarmId = 20;
        var command = new UpdatePaddockCommand(paddockId, "Name", null, null, null);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Old Paddock",
            FarmId = paddockFarmId,
        };

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act & Assert
        await Should.ThrowAsync<ForbiddenAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_PaddockNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new UpdatePaddockCommand(999, "Name", 1, null, null);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Paddock?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_UpdateAreaWithoutType_ThrowsArgumentException()
    {
        // Arrange
        var paddockId = 1;
        var farmId = 1;
        var command = new UpdatePaddockCommand(paddockId, null, null, 10.5m, null);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test",
            FarmId = farmId,
            AreaType = null,
        };

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_UpdateInvalidAreaType_ThrowsArgumentException()
    {
        // Arrange
        var paddockId = 1;
        var farmId = 1;
        var command = new UpdatePaddockCommand(paddockId, null, null, null, "InvalidType");
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test",
            FarmId = farmId,
        };

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
