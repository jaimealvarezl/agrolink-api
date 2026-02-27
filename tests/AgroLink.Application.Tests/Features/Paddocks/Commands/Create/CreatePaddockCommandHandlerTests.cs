using System.Linq.Expressions;
using AgroLink.Application.Features.Paddocks.Commands.Create;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Commands.Create;

[TestFixture]
public class CreatePaddockCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<CreatePaddockCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private CreatePaddockCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidCreatePaddockCommand_ReturnsPaddockDto()
    {
        // Arrange
        var userId = 10;
        var farmId = 1;
        var name = "Test Paddock";
        var area = 10.5m;
        var areaType = "Hectare";
        var command = new CreatePaddockCommand(name, farmId, userId, area, areaType);

        var farm = new Farm { Id = farmId, Name = "Test Farm" };
        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(member);

        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.AddAsync(It.IsAny<Paddock>()))
            .Callback<Paddock>(p => p.Id = 1);
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Name.ShouldBe(name);
        result.FarmName.ShouldBe(farm.Name);
        result.Area.ShouldBe(area);
        result.AreaType.ShouldBe(areaType);
        _mocker
            .GetMock<IPaddockRepository>()
            .Verify(r => r.AddAsync(It.IsAny<Paddock>()), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_FarmNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new CreatePaddockCommand("Test", 99, 1, null, null);
        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(99)).ReturnsAsync((Farm?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_UserNotMember_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new CreatePaddockCommand("Test", farmId, userId, null, null);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(farmId))
            .ReturnsAsync(new Farm { Id = farmId });
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync((FarmMember?)null);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_AreaProvidedWithoutType_ThrowsArgumentException()
    {
        // Arrange
        const int userId = 10;
        const int farmId = 1;
        var command = new CreatePaddockCommand("Test", farmId, userId, 10.5m, null);

        var farm = new Farm { Id = farmId, Name = "Test Farm" };
        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(member);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_InvalidAreaType_ThrowsArgumentException()
    {
        // Arrange
        const int userId = 10;
        const int farmId = 1;
        var command = new CreatePaddockCommand("Test", farmId, userId, 10.5m, "InvalidType");

        var farm = new Farm { Id = farmId, Name = "Test Farm" };
        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(member);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
    }
}
