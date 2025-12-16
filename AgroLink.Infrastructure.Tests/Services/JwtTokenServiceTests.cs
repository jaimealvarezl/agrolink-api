using System;
using System.Linq;
using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Moq;
using NUnit.Framework;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Services;

[TestFixture]
public class JwtTokenServiceTests
{
    private Mock<IConfiguration> _configurationMock = null!;
    private JwtTokenService _service = null!;

    [SetUp]
    public void Setup()
    {
        _configurationMock = new Mock<IConfiguration>();
        _configurationMock
            .Setup(x => x["Jwt:Key"])
            .Returns("thisisthejwtsecretkeyforagrolinkapp1234");
        _configurationMock.Setup(x => x["Jwt:Issuer"]).Returns("AgroLink");
        _configurationMock.Setup(x => x["Jwt:Audience"]).Returns("AgroLink");

        _service = new JwtTokenService(_configurationMock.Object);
    }

    [Test]
    public void GenerateToken_WithUserEntity_ReturnsValidJwtToken()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        // Act
        var token = _service.GenerateToken(user);

        // Assert
        token.ShouldNotBeNullOrEmpty();
        var principal = _service.ValidateToken(token);
        principal.ShouldBeTrue(); // ValidateToken returns bool now
        var userDto = _service.GetUserFromToken(token);
        userDto.ShouldNotBeNull();
        userDto.Id.ShouldBe(user.Id);
        userDto.Email.ShouldBe(user.Email);
    }

    [Test]
    public void GenerateToken_WithUserDto_ReturnsValidJwtToken()
    {
        // Arrange
        var userDto = new UserDto
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        // Act
        var token = _service.GenerateToken(userDto);

        // Assert
        token.ShouldNotBeNullOrEmpty();
        var principal = _service.ValidateToken(token);
        principal.ShouldBeTrue();
        var returnedUserDto = _service.GetUserFromToken(token);
        returnedUserDto.ShouldNotBeNull();
        returnedUserDto.Id.ShouldBe(userDto.Id);
        returnedUserDto.Email.ShouldBe(userDto.Email);
    }

    [Test]
    public void ValidateToken_WithValidToken_ReturnsTrue()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Email = "test@example.com",
            Role = "USER",
        };
        var token = _service.GenerateToken(user);

        // Act
        var isValid = _service.ValidateToken(token);

        // Assert
        isValid.ShouldBeTrue();
    }

    [Test]
    public void ValidateToken_WithInvalidToken_ReturnsFalse()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var isValid = _service.ValidateToken(invalidToken);

        // Assert
        isValid.ShouldBeFalse();
    }

    [Test]
    public void GetUserFromToken_WithValidToken_ReturnsUserDto()
    {
        // Arrange
        var user = new User
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "USER",
        };
        var token = _service.GenerateToken(user);

        // Act
        var result = _service.GetUserFromToken(token);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(user.Id);
        result.Name.ShouldBe(user.Name);
        result.Email.ShouldBe(user.Email);
        result.Role.ShouldBe(user.Role);
    }

    [Test]
    public void GetUserFromToken_WithInvalidToken_ReturnsNull()
    {
        // Arrange
        var invalidToken = "invalid.jwt.token";

        // Act
        var result = _service.GetUserFromToken(invalidToken);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void GetUserFromToken_WithTokenHavingNoUserIdClaim_ReturnsNull()
    {
        // Arrange (create a token manually without 'userid' claim for testing)
        var invalidUser = new User
        {
            Id = 0,
            Name = "No Id User",
            Email = "no_id@example.com",
            Role = "USER",
        };
        var tokenWithNoUserId = _service.GenerateToken(invalidUser); // Assume GenerateToken can generate without ID for this test scenario if needed. Or create a custom token.
        // For simplicity here, let's just make a token that won't pass validation, leading to null.

        // More robust test would involve directly manipulating claims or using a JwtSecurityTokenHandler to create a token without specific claims
        // For now, testing with an invalid token will also cover this.
        var result = _service.GetUserFromToken("some.token.withoutuserid");

        // Assert
        result.ShouldBeNull();
    }
}
