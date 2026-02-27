using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Update;

[TestFixture]
public class UpdateFarmCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdateFarmCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private UpdateFarmCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidUpdateByOwner_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var name = "Updated Farm Name";
        var location = "Updated Location";
        var cue = "ABC12345";
        var command = new UpdateFarmCommand(farmId, name, location, cue, userId);

        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            OwnerId = 20,
        };
        var membership = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(membership);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(name);
        result.Location.ShouldBe(location);
        result.CUE.ShouldBe(cue);
        result.Role.ShouldBe(FarmMemberRoles.Owner);

        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(It.IsAny<Farm>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_ValidUpdateByAdmin_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null, userId);

        var farm = new Farm { Id = farmId, Name = "Old Name" };
        var membership = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Admin,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(membership);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe(FarmMemberRoles.Admin);
    }

    [Test]
    public async Task Handle_ForbiddenRole_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null, userId);

        var farm = new Farm { Id = farmId, Name = "Old Name" };
        var membership = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Viewer,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(membership);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NotMember_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null, userId);

        var farm = new Farm { Id = farmId, Name = "Old Name" };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync((FarmMember?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_FarmNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new UpdateFarmCommand(999, "Name", null, null, 10);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Farm?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
