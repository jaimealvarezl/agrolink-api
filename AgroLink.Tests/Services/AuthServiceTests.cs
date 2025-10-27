using System.Security.Claims;
using System.Text;
using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Moq;
using Shouldly;

namespace AgroLink.Tests.Services;

[TestFixture]
public class AuthServiceTests
{
    private Mock<AgroLinkDbContext> _contextMock = null!;
    private Mock<IConfiguration> _configurationMock = null!;
    private AuthService _service = null!;

    [SetUp]
    public void Setup()
    {
        _contextMock = new Mock<AgroLinkDbContext>(new DbContextOptions<AgroLinkDbContext>());
        _configurationMock = new Mock<IConfiguration>();

        // Setup configuration mock
        _configurationMock
            .Setup(x => x["Jwt:SecretKey"])
            .Returns("test-secret-key-that-is-long-enough-for-hmac-sha256");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("AgroLink-Test");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("AgroLink-Test");
        _configurationMock.Setup(x => x["Jwt:ExpiryMinutes"]).Returns("60");

        _service = new AuthService(_contextMock.Object, _configurationMock.Object);
    }

    [Test]
    public async Task LoginAsync_WithValidCredentials_ShouldReturnAuthResponse()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(loginDto.Password);
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = loginDto.Email,
            PasswordHash = hashedPassword,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var usersDbSet = new Mock<DbSet<User>>();
        usersDbSet
            .Setup(x =>
                x.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(user);

        _contextMock.Setup(x => x.Users).Returns(usersDbSet.Object);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldNotBeNullOrEmpty();
        result.User.ShouldNotBeNull();
        result.User.Email.ShouldBe(loginDto.Email);
        result.User.Name.ShouldBe("Test User");
    }

    [Test]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "password123" };

        var usersDbSet = new Mock<DbSet<User>>();
        usersDbSet
            .Setup(x =>
                x.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((User?)null);

        _contextMock.Setup(x => x.Users).Returns(usersDbSet.Object);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task LoginAsync_WithInvalidPassword_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };

        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctpassword");
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = loginDto.Email,
            PasswordHash = hashedPassword,
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var usersDbSet = new Mock<DbSet<User>>();
        usersDbSet
            .Setup(x =>
                x.FirstOrDefaultAsync(
                    It.IsAny<System.Linq.Expressions.Expression<Func<User, bool>>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(user);

        _contextMock.Setup(x => x.Users).Returns(usersDbSet.Object);

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task ValidateTokenAsync_WithValidToken_ShouldReturnTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        // Create a valid token
        var token = CreateValidJwtToken(user);

        // Act
        var result = await _service.ValidateTokenAsync(token);

        // Assert
        result.ShouldBeTrue();
    }

    [Test]
    public async Task ValidateTokenAsync_WithInvalidToken_ShouldReturnFalse()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _service.ValidateTokenAsync(invalidToken);

        // Assert
        result.ShouldBeFalse();
    }

    [Test]
    public async Task GetUserFromTokenAsync_WithValidToken_ShouldReturnUser()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var token = CreateValidJwtToken(user);

        // Act
        var result = await _service.GetUserFromTokenAsync(token);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Email.ShouldBe("test@example.com");
    }

    [Test]
    public async Task GetUserFromTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _service.GetUserFromTokenAsync(invalidToken);

        // Assert
        result.ShouldBeNull();
    }

    private string CreateValidJwtToken(User user)
    {
        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes("test-secret-key-that-is-long-enough-for-hmac-sha256");

        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                }
            ),
            Expires = DateTime.UtcNow.AddDays(7),
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
