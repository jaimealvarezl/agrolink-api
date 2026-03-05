using System.Linq.Expressions;
using AgroLink.Application.Features.Auth.Commands.UpdateProfile;
using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Auth.Commands.UpdateProfile;

[TestFixture]
public class UpdateProfileCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<UpdateProfileCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private UpdateProfileCommandHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_UpdatesUserAndOwner()
    {
        // Arrange
        var userId = 1;
        var oldName = "Old Name";
        var newName = "New Name";
        var request = new UpdateProfileRequest { Name = newName };
        var command = new UpdateProfileCommand(request);

        var user = new User
        {
            Id = userId,
            Name = oldName,
            Email = "test@example.com",
            Role = "USER",
        };
        var owner = new Owner
        {
            Id = 10,
            Name = oldName,
            UserId = userId,
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(userId);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(new List<Owner> { owner });
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(newName);
        user.Name.ShouldBe(newName);
        owner.Name.ShouldBe(newName);

        _mocker.GetMock<IUserRepository>().Verify(r => r.Update(user), Times.Once);
        _mocker.GetMock<IOwnerRepository>().Verify(r => r.Update(owner), Times.Once);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_ValidRequestNoOwner_UpdatesUserOnly()
    {
        // Arrange
        var userId = 1;
        var oldName = "Old Name";
        var newName = "New Name";
        var request = new UpdateProfileRequest { Name = newName };
        var command = new UpdateProfileCommand(request);

        var user = new User
        {
            Id = userId,
            Name = oldName,
            Email = "test@example.com",
            Role = "USER",
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(userId);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(new List<Owner>());
        _mocker.GetMock<IUnitOfWork>().Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(newName);
        user.Name.ShouldBe(newName);

        _mocker.GetMock<IUserRepository>().Verify(r => r.Update(user), Times.Once);
        _mocker.GetMock<IOwnerRepository>().Verify(r => r.Update(It.IsAny<Owner>()), Times.Never);
        _mocker.GetMock<IUnitOfWork>().Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateProfileRequest { Name = "New Name" };
        var command = new UpdateProfileCommand(request);

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.GetRequiredUserId()).Returns(userId);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.GetByIdAsync(userId))
            .ReturnsAsync((User?)null);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
