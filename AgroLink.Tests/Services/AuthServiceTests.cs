using System.Security.Claims;
using System.Text;
using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Shouldly;

namespace AgroLink.Tests.Services;

[TestFixture]
public class AuthServiceTests : TestBase
{
    private AgroLinkDbContext _context = null!;
    private IConfiguration _configuration = null!;
    private AuthService _service = null!;

    [SetUp]
    public void Setup()
    {
        _context = CreateInMemoryContext();
        _configuration = TestConfiguration.CreateConfiguration();
        _service = new AuthService(_context, _configuration);
    }

    [TearDown]
    public void TearDown()
    {
        _context?.Dispose();
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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.LoginAsync(loginDto);

        // Assert
        result.ShouldNotBeNull();
        result.Token.ShouldNotBeNullOrEmpty();
        result.User.ShouldNotBeNull();
        result.User.Email.ShouldBe(loginDto.Email);
    }

    [Test]
    public async Task LoginAsync_WithInvalidEmail_ShouldReturnNull()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "nonexistent@example.com", Password = "password123" };

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

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

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
            PasswordHash = "hashedpassword",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

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
        var invalidToken = "invalid.jwt.token";

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
            PasswordHash = "hashedpassword",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = CreateValidJwtToken(user);

        // Act
        var result = await _service.GetUserFromTokenAsync(token);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        result.Email.ShouldBe(user.Email);
    }

    [Test]
    public async Task GetUserFromTokenAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var result = await _service.GetUserFromTokenAsync(invalidToken);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task RegisterAsync_WithValidData_ShouldReturnUser()
    {
        // Arrange
        var userDto = new UserDto
        {
            Name = "New User",
            Email = "newuser@example.com",
            Role = "User",
        };
        var password = "password123";

        // Act
        var result = await _service.RegisterAsync(userDto, password);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe(userDto.Name);
        result.Email.ShouldBe(userDto.Email);
        result.Role.ShouldBe(userDto.Role);

        // Verify user was saved to database
        var savedUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == userDto.Email);
        savedUser.ShouldNotBeNull();
        savedUser.Name.ShouldBe(userDto.Name);
    }

    [Test]
    public async Task RegisterAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var existingUser = new User
        {
            Id = 1,
            Name = "Existing User",
            Email = "existing@example.com",
            PasswordHash = "hashedpassword",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var userDto = new UserDto
        {
            Name = "New User",
            Email = "existing@example.com", // Same email
            Role = "User",
        };
        var password = "password123";

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _service.RegisterAsync(userDto, password)
        );
    }

    private string CreateValidJwtToken(User user)
    {
        var secretKey = _configuration["Jwt:Key"] ?? "test-secret-key";
        var issuer = _configuration["Jwt:Issuer"] ?? "AgroLink-Test";
        var audience = _configuration["Jwt:Audience"] ?? "AgroLink-Test";
        var expiryMinutes = int.Parse(_configuration["Jwt:ExpiryMinutes"] ?? "60");

        var key = Encoding.UTF8.GetBytes(secretKey);
        var tokenDescriptor = new Microsoft.IdentityModel.Tokens.SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                new[]
                {
                    new Claim("userid", user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Name),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, user.Role),
                }
            ),
            Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new Microsoft.IdentityModel.Tokens.SigningCredentials(
                new Microsoft.IdentityModel.Tokens.SymmetricSecurityKey(key),
                Microsoft.IdentityModel.Tokens.SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var tokenHandler = new System.IdentityModel.Tokens.Jwt.JwtSecurityTokenHandler();
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Test]
    public async Task RegisterUserAsync_WithValidRequest_ShouldReturnUserDto()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "password123",
            Role = "ADMIN",
        };

        // Act
        var result = await _service.RegisterUserAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test User");
        result.Email.ShouldBe("test@example.com");
        result.Role.ShouldBe("ADMIN");
        result.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task RegisterUserAsync_WithNullRole_ShouldUseDefaultRole()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "password123",
            Role = null,
        };

        // Act
        var result = await _service.RegisterUserAsync(request);

        // Assert
        result.ShouldNotBeNull();
        result.Role.ShouldBe("USER");
    }

    [Test]
    public async Task RegisterUserAsync_WithExistingEmail_ShouldThrowException()
    {
        // Arrange
        var existingUser = new User
        {
            Name = "Existing User",
            Email = "existing@example.com",
            PasswordHash = "hashed",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _context.Users.Add(existingUser);
        await _context.SaveChangesAsync();

        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "existing@example.com",
            Password = "password123",
        };

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(async () =>
            await _service.RegisterUserAsync(request)
        );
    }

    [Test]
    public async Task GetUserProfileAsync_WithValidToken_ShouldReturnUserDto()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = CreateValidJwtToken(user);

        // Act
        var result = await _service.GetUserProfileAsync(token);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Test User");
        result.Email.ShouldBe("test@example.com");
        result.Role.ShouldBe("USER");
    }

    [Test]
    public async Task GetUserProfileAsync_WithInvalidToken_ShouldReturnNull()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _service.GetUserProfileAsync(invalidToken);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetUserProfileAsync_WithEmptyToken_ShouldReturnNull()
    {
        // Arrange
        var emptyToken = "";

        // Act
        var result = await _service.GetUserProfileAsync(emptyToken);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetUserProfileAsync_WithNullToken_ShouldReturnNull()
    {
        // Arrange
        string? nullToken = null;

        // Act
        var result = await _service.GetUserProfileAsync(nullToken!);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task ValidateTokenResponseAsync_WithValidToken_ShouldReturnValidTrue()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hashed",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        var token = CreateValidJwtToken(user);

        // Act
        var result = await _service.ValidateTokenResponseAsync(token);

        // Assert
        result.ShouldNotBeNull();
        result.Valid.ShouldBeTrue();
    }

    [Test]
    public async Task ValidateTokenResponseAsync_WithInvalidToken_ShouldReturnValidFalse()
    {
        // Arrange
        var invalidToken = "invalid.token.here";

        // Act
        var result = await _service.ValidateTokenResponseAsync(invalidToken);

        // Assert
        result.ShouldNotBeNull();
        result.Valid.ShouldBeFalse();
    }
}
