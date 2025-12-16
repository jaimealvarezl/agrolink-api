using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Auth.Commands.Login;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Auth.Commands.Login;

[TestFixture]
public class LoginCommandHandlerTests
{
    private Mock<IAuthRepository> _authRepositoryMock = null!;
    private Mock<IJwtTokenService> _jwtTokenServiceMock = null!;
    private Mock<IPasswordHasher> _passwordHasherMock = null!;
    private LoginCommandHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _authRepositoryMock = new Mock<IAuthRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _handler = new LoginCommandHandler(
            _authRepositoryMock.Object,
            _jwtTokenServiceMock.Object,
            _passwordHasherMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidCredentials_ReturnsAuthResponse()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
        var command = new LoginCommand(loginDto);
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            IsActive = true,
        };
        var authResponseToken = "some_jwt_token";

        _authRepositoryMock.Setup(r => r.GetUserByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword(loginDto.Password, user.PasswordHash))
            .Returns(true);
        _jwtTokenServiceMock.Setup(s => s.GenerateToken(user)).Returns(authResponseToken);
        _authRepositoryMock.Setup(r => r.UpdateUserAsync(user)).Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe(authResponseToken);
        result.User.Email.ShouldBe(user.Email);
        _authRepositoryMock.Verify(
            r => r.UpdateUserAsync(It.Is<User>(u => u.LastLoginAt != default(DateTime))),
            Times.Once
        );
    }

    [Test]
    public async Task Handle_InvalidEmail_ReturnsNull()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "password123" };
        var command = new LoginCommand(loginDto);

        _authRepositoryMock
            .Setup(r => r.GetUserByEmailAsync(loginDto.Email))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        _authRepositoryMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _passwordHasherMock.Verify(
            h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
        _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task Handle_InactiveUser_ReturnsNull()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "inactive@example.com", Password = "password123" };
        var command = new LoginCommand(loginDto);
        var user = new User
        {
            Id = 2,
            Name = "Inactive User",
            Email = "inactive@example.com",
            PasswordHash = "hashed_password",
            IsActive = false,
        };

        _authRepositoryMock.Setup(r => r.GetUserByEmailAsync(loginDto.Email)).ReturnsAsync(user);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        _authRepositoryMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _passwordHasherMock.Verify(
            h => h.VerifyPassword(It.IsAny<string>(), It.IsAny<string>()),
            Times.Never
        );
        _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.IsAny<User>()), Times.Never);
    }

    [Test]
    public async Task Handle_InvalidPassword_ReturnsNull()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "wrong_password" };
        var command = new LoginCommand(loginDto);
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed_password",
            IsActive = true,
        };

        _authRepositoryMock.Setup(r => r.GetUserByEmailAsync(loginDto.Email)).ReturnsAsync(user);
        _passwordHasherMock
            .Setup(h => h.VerifyPassword(loginDto.Password, user.PasswordHash))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        _authRepositoryMock.Verify(r => r.UpdateUserAsync(It.IsAny<User>()), Times.Never);
        _jwtTokenServiceMock.Verify(s => s.GenerateToken(It.IsAny<User>()), Times.Never);
    }
}
