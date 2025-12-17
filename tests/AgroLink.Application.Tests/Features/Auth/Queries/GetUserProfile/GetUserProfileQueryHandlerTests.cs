using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Auth.Queries.GetUserProfile;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Auth.Queries.GetUserProfile;

[TestFixture]
public class GetUserProfileQueryHandlerTests
{
    private Mock<IAuthRepository> _authRepositoryMock = null!;
    private Mock<IJwtTokenService> _jwtTokenServiceMock = null!;
    private GetUserProfileQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _authRepositoryMock = new Mock<IAuthRepository>();
        _jwtTokenServiceMock = new Mock<IJwtTokenService>();
        _handler = new GetUserProfileQueryHandler(
            _authRepositoryMock.Object,
            _jwtTokenServiceMock.Object
        );
    }

    [Test]
    public async Task Handle_ValidTokenAndUser_ReturnsUserDto()
    {
        // Arrange
        var token = "valid_jwt_token";
        var query = new GetUserProfileQuery(token);
        var userDtoFromToken = new UserDto { Id = 1, Email = "test@example.com" };
        var userEntity = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
            LastLoginAt = DateTime.UtcNow,
        };

        _jwtTokenServiceMock.Setup(s => s.GetUserFromToken(token)).Returns(userDtoFromToken);
        _authRepositoryMock
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

        _jwtTokenServiceMock.Setup(s => s.GetUserFromToken(token)).Returns((UserDto?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
        _authRepositoryMock.Verify(r => r.GetUserByIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Test]
    public async Task Handle_ValidTokenUserNotFoundInRepo_ReturnsNull()
    {
        // Arrange
        var token = "valid_jwt_token";
        var query = new GetUserProfileQuery(token);
        var userDtoFromToken = new UserDto { Id = 1, Email = "test@example.com" };

        _jwtTokenServiceMock.Setup(s => s.GetUserFromToken(token)).Returns(userDtoFromToken);
        _authRepositoryMock
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
        var userDtoFromToken = new UserDto { Id = 1, Email = "test@example.com" };
        var inactiveUser = new User { Id = 1, IsActive = false };

        _jwtTokenServiceMock.Setup(s => s.GetUserFromToken(token)).Returns(userDtoFromToken);
        _authRepositoryMock
            .Setup(r => r.GetUserByIdAsync(userDtoFromToken.Id))
            .ReturnsAsync(inactiveUser);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
