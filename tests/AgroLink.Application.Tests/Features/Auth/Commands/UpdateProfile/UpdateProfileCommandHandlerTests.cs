using System.Linq.Expressions;
using AgroLink.Application.Features.Auth.Commands.UpdateProfile;
using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Auth.Commands.UpdateProfile;

[TestFixture]
public class UpdateProfileCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _userRepositoryMock = new Mock<IUserRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _handler = new UpdateProfileCommandHandler(
            _userRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _currentUserServiceMock.Object,
            _unitOfWorkMock.Object
        );
    }

    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private Mock<IUnitOfWork> _unitOfWorkMock = null!;
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

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(newName);
        user.Name.ShouldBe(newName);
        owner.Name.ShouldBe(newName);

        _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
        _ownerRepositoryMock.Verify(r => r.Update(owner), Times.Once);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
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

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync(user);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync((Owner?)null);
        _unitOfWorkMock.Setup(u => u.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(newName);
        user.Name.ShouldBe(newName);

        _userRepositoryMock.Verify(r => r.Update(user), Times.Once);
        _ownerRepositoryMock.Verify(r => r.Update(It.IsAny<Owner>()), Times.Never);
        _unitOfWorkMock.Verify(u => u.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task Handle_UserNotFound_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var userId = 1;
        var request = new UpdateProfileRequest { Name = "New Name" };
        var command = new UpdateProfileCommand(request);

        _currentUserServiceMock.Setup(s => s.GetRequiredUserId()).Returns(userId);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(userId)).ReturnsAsync((User?)null);

        // Act & Assert
        await Should.ThrowAsync<UnauthorizedAccessException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
    }
}
