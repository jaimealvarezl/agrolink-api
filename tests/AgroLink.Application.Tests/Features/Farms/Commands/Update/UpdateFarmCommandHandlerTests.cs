using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Application.Interfaces;
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
    public async Task Handle_ValidUpdate_ReturnsFarmDto()
    {
        var farmId = 1;
        var name = "Updated Farm Name";
        var location = "Updated Location";
        var cue = "ABC12345";
        var command = new UpdateFarmCommand(farmId, name, location, cue, 10);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            OwnerId = 20,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<ICurrentUserService>()
            .Setup(s => s.CurrentFarmRole)
            .Returns(FarmMemberRoles.Owner);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        var result = await _handler.Handle(command, CancellationToken.None);

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
    public async Task Handle_NullOptionalFields_DoesNotOverwriteExistingValues()
    {
        var farmId = 1;
        var command = new UpdateFarmCommand(farmId, "New Name", null, null, 10);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Old Name",
            Location = "Keep This",
            CUE = "Keep This",
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<ICurrentUserService>()
            .Setup(s => s.CurrentFarmRole)
            .Returns(FarmMemberRoles.Owner);

        var result = await _handler.Handle(command, CancellationToken.None);

        result.Location.ShouldBe("Keep This");
        result.CUE.ShouldBe("Keep This");
    }

    [Test]
    public async Task Handle_FarmNotFound_ThrowsArgumentException()
    {
        var command = new UpdateFarmCommand(999, "Name", null, null, 10);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Farm?)null);

        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
