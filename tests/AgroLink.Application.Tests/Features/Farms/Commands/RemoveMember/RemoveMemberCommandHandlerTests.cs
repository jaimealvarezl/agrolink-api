using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Commands.RemoveMember;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.RemoveMember;

[TestFixture]
public class RemoveMemberCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<RemoveMemberCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private RemoveMemberCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_RemovesMember()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new RemoveMemberCommand(farmId, userId);

        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Viewer,
        };

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(farmId, userId))
            .ReturnsAsync(member);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<IFarmMemberRepository>().Verify(r => r.Remove(member), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_RemovingLastOwner_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new RemoveMemberCommand(farmId, userId);

        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(farmId, userId))
            .ReturnsAsync(member);

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(1);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Cannot remove the last owner");
    }

    [Test]
    public async Task Handle_RemovingOneOfSeveralOwners_Succeeds()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new RemoveMemberCommand(farmId, userId);

        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(farmId, userId))
            .ReturnsAsync(member);

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.CountAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(2);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert
        _mocker.GetMock<IFarmMemberRepository>().Verify(r => r.Remove(member), Times.Once);
    }
}
