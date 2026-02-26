using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Features.Auth.Queries.GetUserProfile;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Auth.Queries.GetUserProfile;

[TestFixture]
public class GetUserProfileQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetUserProfileQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetUserProfileQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ValidTokenAndUser_ReturnsUserDto()
    {
        // Arrange
        var token = "valid_jwt_token";
        var query = new GetUserProfileQuery(token);
        var userDtoFromToken = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var userEntity = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };

        _mocker
            .GetMock<IJwtTokenService>()
            .Setup(s => s.GetUserFromToken(token))
            .Returns(userDtoFromToken);
        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByIdAsync(userDtoFromToken.Id))
            .ReturnsAsync(userEntity);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(userEntity.Id);
        result.Email.ShouldBe(userEntity.Email);
    }

    [Test]
    public async Task Handle_InvalidToken_ReturnsNull()
    {
        // Arrange
        var token = "invalid_jwt_token";
        var query = new GetUserProfileQuery(token);

        _mocker
            .GetMock<IJwtTokenService>()
            .Setup(s => s.GetUserFromToken(token))
            .Returns((UserDto?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        _mocker
            .GetMock<IAuthRepository>()
            .Verify(r => r.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Handle_ValidTokenUserNotFoundInRepo_ReturnsNull()
    {
        // Arrange
        var token = "valid_jwt_token";
        var query = new GetUserProfileQuery(token);
        var userDtoFromToken = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _mocker
            .GetMock<IJwtTokenService>()
            .Setup(s => s.GetUserFromToken(token))
            .Returns(userDtoFromToken);
        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByIdAsync(userDtoFromToken.Id))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_ValidTokenInactiveUser_ReturnsNull()
    {
        // Arrange
        var token = "valid_jwt_token";
        var query = new GetUserProfileQuery(token);
        var userDtoFromToken = new UserDto
        {
            Id = 1,
            Email = "test@example.com",
            Name = "Test User",
            Role = "User",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        var inactiveUser = new User { Id = 1, IsActive = false };

        _mocker
            .GetMock<IJwtTokenService>()
            .Setup(s => s.GetUserFromToken(token))
            .Returns(userDtoFromToken);
        _mocker
            .GetMock<IAuthRepository>()
            .Setup(r => r.GetUserByIdAsync(userDtoFromToken.Id))
            .ReturnsAsync(inactiveUser);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
