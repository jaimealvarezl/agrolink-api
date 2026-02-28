using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Farms.Commands.UpdateMemberRole;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.UpdateMemberRole;

[TestFixture]
public class UpdateMemberRoleCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdateMemberRoleCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private UpdateMemberRoleCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_UpdatesRole()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var newRole = FarmMemberRoles.Admin;
        var command = new UpdateMemberRoleCommand(farmId, userId, newRole, 99);

        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Viewer,
            User = new User { Name = "Test", Email = "test@example.com" },
        };

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(farmId, userId, true))
            .ReturnsAsync(member);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Role.ShouldBe(newRole);
        member.Role.ShouldBe(newRole);
        _mocker.GetMock<IFarmMemberRepository>().Verify(r => r.Update(member), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_OwnerDowngradingSelf_ThrowsArgumentException()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var command = new UpdateMemberRoleCommand(farmId, userId, FarmMemberRoles.Viewer, userId);

        var member = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(farmId, userId, true))
            .ReturnsAsync(member);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("Owners cannot downgrade their own role");
    }

    [Test]
    public async Task Handle_MemberNotFound_ThrowsNotFoundException()
    {
        // Arrange
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<bool>()))
            .ReturnsAsync((FarmMember?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(async () =>
            await _handler.Handle(
                new UpdateMemberRoleCommand(1, 1, "Role", 1),
                CancellationToken.None
            )
        );
    }
}
