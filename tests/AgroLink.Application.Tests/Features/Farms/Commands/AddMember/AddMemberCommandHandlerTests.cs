using AgroLink.Application.Features.Farms.Commands.AddMember;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Commands.AddMember;

[TestFixture]
public class AddMemberCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<AddMemberCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private AddMemberCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_AddsMember()
    {
        // Arrange
        var farmId = 1;
        var email = "test@example.com";
        var role = FarmMemberRoles.Editor;
        var command = new AddMemberCommand(farmId, email, role);

        var user = new User
        {
            Id = 10,
            Email = email,
            Name = "Test User",
        };

        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(farmId, user.Id))
            .ReturnsAsync((FarmMember?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.UserId.ShouldBe(user.Id);
        result.Role.ShouldBe(role);

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Verify(
                r =>
                    r.AddAsync(
                        It.Is<FarmMember>(m =>
                            m.FarmId == farmId && m.UserId == user.Id && m.Role == role
                        )
                    ),
                Times.Once
            );
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_UserNotFound_ThrowsArgumentException()
    {
        // Arrange
        var command = new AddMemberCommand(1, "nonexistent@example.com", FarmMemberRoles.Viewer);

        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((User?)null);

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("User not found");
    }

    [Test]
    public async Task Handle_UserAlreadyMember_ThrowsArgumentException()
    {
        // Arrange
        var email = "member@example.com";
        var command = new AddMemberCommand(1, email, FarmMemberRoles.Viewer);
        var user = new User { Id = 10, Email = email };

        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByEmailAsync(email)).ReturnsAsync(user);

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.GetByFarmAndUserAsync(1, 10))
            .ReturnsAsync(new FarmMember());

        // Act & Assert
        var ex = await Should.ThrowAsync<ArgumentException>(async () =>
            await _handler.Handle(command, CancellationToken.None)
        );
        ex.Message.ShouldContain("already a member");
    }
}
