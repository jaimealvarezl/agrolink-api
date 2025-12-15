using AgroLink.Application.Interfaces;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Services;
using Shouldly;

namespace AgroLink.Tests.Services;

[TestFixture]
public class TokenExtractionServiceTests
{
    [SetUp]
    public void Setup()
    {
        _service = new TokenExtractionService();
    }

    private ITokenExtractionService _service = null!;

    [Test]
    public void ExtractTokenFromHeader_WithValidBearerToken_ShouldReturnToken()
    {
        // Arrange
        var authHeader = "Bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
    }

    [Test]
    public void ExtractTokenFromHeader_WithBearerTokenWithSpaces_ShouldReturnTrimmedToken()
    {
        // Arrange
        var authHeader = "Bearer   eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...   ";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
    }

    [Test]
    public void ExtractTokenFromHeader_WithLowerCaseBearer_ShouldReturnToken()
    {
        // Arrange
        var authHeader = "bearer eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
    }

    [Test]
    public void ExtractTokenFromHeader_WithMixedCaseBearer_ShouldReturnToken()
    {
        // Arrange
        var authHeader = "BeArEr eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...");
    }

    [Test]
    public void ExtractTokenFromHeader_WithoutBearerPrefix_ShouldReturnNull()
    {
        // Arrange
        var authHeader = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractTokenFromHeader_WithEmptyString_ShouldReturnNull()
    {
        // Arrange
        var authHeader = "";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractTokenFromHeader_WithNullString_ShouldReturnNull()
    {
        // Arrange
        string? authHeader = null;

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader!);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public void ExtractTokenFromHeader_WithOnlyBearer_ShouldReturnEmptyString()
    {
        // Arrange
        var authHeader = "Bearer";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("");
    }

    [Test]
    public void ExtractTokenFromHeader_WithBearerAndSpacesOnly_ShouldReturnEmptyString()
    {
        // Arrange
        var authHeader = "Bearer   ";

        // Act
        var result = _service.ExtractTokenFromHeader(authHeader);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBe("");
    }
}
