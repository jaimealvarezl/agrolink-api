using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.Delete;

[TestFixture]
public class DeleteFarmCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<DeleteFarmCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private DeleteFarmCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingFarmByOwner_SoftDeletesFarm()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);
        var farm = new Farm { Id = farmId, IsActive = true };
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
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        farm.IsActive.ShouldBeFalse();
        farm.DeletedAt.ShouldNotBeNull();
        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(farm), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_NotOwner_ReturnsSuccessfullyToPreventInformationLeakage()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);
        var farm = new Farm { Id = farmId, IsActive = true };
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
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_NonExistingFarm_ReturnsSuccessfullyForIdempotency()
    {
        // Arrange
        var farmId = 999;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(farmId))
            .ReturnsAsync((Farm?)null);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_AlreadyDeletedFarm_ReturnsSuccessfullyForIdempotency()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new DeleteFarmCommand(farmId, userId);
        var farm = new Farm { Id = farmId, IsActive = false };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<IFarmRepository>().Verify(r => r.Update(It.IsAny<Farm>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Never);
    }
}
