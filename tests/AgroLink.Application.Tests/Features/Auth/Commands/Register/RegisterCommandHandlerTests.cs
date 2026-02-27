using AgroLink.Application.Features.Auth.Commands.Register;
using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Auth.Commands.Register;

[TestFixture]
public class RegisterCommandHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<RegisterCommandHandler>();
    }

    private AutoMocker _mocker = null!;
    private RegisterCommandHandler _handler = null!;

    [Test]
    public async Task Handle_NewUser_ReturnsAuthResponse()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "New User",
            Email = "new@example.com",
            Password = "password123",
            Role = "USER",
        };
        var command = new RegisterCommand(registerRequest);
        var hashedPassword = "hashed_password";
        var authResponseToken = "new_jwt_token";

        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByEmailAsync(registerRequest.Email))
            .ReturnsAsync((User?)null);
        _mocker
            .GetMock<IPasswordHasher>()
            .Setup(h => h.HashPassword(registerRequest.Password))
            .Returns(hashedPassword);
        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.AddUserAsync(It.IsAny<User>()))
            .Returns(Task.CompletedTask);
        _mocker
            .GetMock<IJwtTokenService>()
            .Setup(s => s.GenerateToken(It.IsAny<UserDto>()))
            .Returns(authResponseToken);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldBe(authResponseToken);
        result.User.Email.ShouldBe(registerRequest.Email);
        _mocker
            .GetMock<IAuthRepository>()
            .Verify(
                r =>
                    r.AddUserAsync(
                        It.Is<User>(u =>
                            u.Email == registerRequest.Email && u.PasswordHash == hashedPassword
                        )
                    ),
                Times.Once
            );
    }

    [Test]
    public async Task Handle_ExistingEmail_ThrowsArgumentException()
    {
        // Arrange
        var registerRequest = new RegisterRequest
        {
            Name = "Existing User",
            Email = "existing@example.com",
            Password = "password123",
            Role = "USER",
        };
        var command = new RegisterCommand(registerRequest);
        var existingUser = new User { Email = registerRequest.Email, IsActive = true };

        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByEmailAsync(registerRequest.Email))
            .ReturnsAsync(existingUser);

        // Act & Assert
        var exception = await Should.ThrowAsync<ArgumentException>(() =>
            _handler.Handle(command, CancellationToken.None)
        );
        exception.Message.ShouldBe("User with this email already exists");
        _mocker
            .GetMock<IAuthRepository>()
            .Verify(r => r.AddUserAsync(It.IsAny<User>()), Times.Never);
        _mocker
            .GetMock<IPasswordHasher>()
            .Verify(h => h.HashPassword(It.IsAny<string>()), Times.Never);
        _mocker
            .GetMock<IJwtTokenService>()
            .Verify(s => s.GenerateToken(It.IsAny<UserDto>()), Times.Never);
    }
}
