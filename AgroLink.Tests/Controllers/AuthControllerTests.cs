using AgroLink.Api.Controllers;
using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Auth.Commands.Login;
using AgroLink.Application.Features.Auth.Commands.Register;
using AgroLink.Application.Features.Auth.Queries.GetUserProfile;
using AgroLink.Application.Features.Auth.Queries.ValidateToken;
using AgroLink.Application.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _tokenExtractionServiceMock = new Mock<ITokenExtractionService>();
        _controller = new AuthController(_tokenExtractionServiceMock.Object, _mediatorMock.Object);
    }

    private Mock<IMediator> _mediatorMock = null!;
    private Mock<ITokenExtractionService> _tokenExtractionServiceMock = null!;
    private AuthController _controller = null!;

    [Test]
    public async Task Login_WithValidCredentials_ShouldReturnOkWithAuthResponse()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "password123" };
        var authResponse = new AuthResponseDto
        {
            Token = "jwt-token",
            User = new UserDto
            {
                Id = 1,
                Name = "Test User",
                Email = "test@example.com",
                Role = "USER",
            },
            ExpiresAt = DateTime.UtcNow.AddHours(1),
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<LoginCommand>(c =>
                        c.LoginDto.Email == loginDto.Email
                        && c.LoginDto.Password == loginDto.Password
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAuthResponse = okResult.Value.ShouldBeOfType<AuthResponseDto>();
        returnedAuthResponse.Token.ShouldBe("jwt-token");
        returnedAuthResponse.User.Name.ShouldBe("Test User");
    }

    [Test]
    public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized()
    {
        // Arrange
        var loginDto = new LoginDto { Email = "test@example.com", Password = "wrongpassword" };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<LoginCommand>(c =>
                        c.LoginDto.Email == loginDto.Email
                        && c.LoginDto.Password == loginDto.Password
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((AuthResponseDto?)null);

        // Act
        var result = await _controller.Login(loginDto);

        // Assert
        result.ShouldNotBeNull();
        var unauthorizedResult = result.Result.ShouldBeOfType<UnauthorizedObjectResult>();
        unauthorizedResult.Value.ShouldBe("Invalid credentials");
    }

    [Test]
    public async Task Register_WithValidRequest_ShouldReturnCreatedWithUser()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "test@example.com",
            Password = "password123",
            Role = "USER",
        };

        var userDto = new UserDto
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        var authResponse = new AuthResponseDto
        {
            Token = "jwt-token",
            User = userDto,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<RegisterCommand>(c =>
                        c.Request.Email == request.Email && c.Request.Password == request.Password
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(authResponse);

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.ShouldNotBeNull();
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var returnedAuthResponse = createdResult.Value.ShouldBeOfType<AuthResponseDto>();
        returnedAuthResponse.User.Name.ShouldBe("Test User");
        returnedAuthResponse.User.Email.ShouldBe("test@example.com");
        returnedAuthResponse.Token.ShouldBe("jwt-token");
    }

    [Test]
    public async Task Register_WithExistingEmail_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new RegisterRequest
        {
            Name = "Test User",
            Email = "existing@example.com",
            Password = "password123",
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<RegisterCommand>(c =>
                        c.Request.Email == request.Email && c.Request.Password == request.Password
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ArgumentException("User with this email already exists"));

        // Act
        var result = await _controller.Register(request);

        // Assert
        result.ShouldNotBeNull();
        var badRequestResult = result.Result.ShouldBeOfType<BadRequestObjectResult>();
        badRequestResult.Value.ShouldBe("User with this email already exists");
    }

    [Test]
    public async Task GetProfile_WithValidToken_ShouldReturnOkWithUser()
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

        _tokenExtractionServiceMock
            .Setup(x => x.ExtractTokenFromHeader(It.IsAny<string>()))
            .Returns("valid-token");

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetUserProfileQuery>(q => q.Token == "valid-token"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(userDto);

        // Setup HTTP context
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer valid-token";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.GetProfile();

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedUser = okResult.Value.ShouldBeOfType<UserDto>();
        returnedUser.Name.ShouldBe("Test User");
    }

    [Test]
    public async Task GetProfile_WithInvalidToken_ShouldReturnUnauthorized()
    {
        // Arrange
        _tokenExtractionServiceMock
            .Setup(x => x.ExtractTokenFromHeader(It.IsAny<string>()))
            .Returns((string?)null);

        // Setup HTTP context
        var httpContext = new DefaultHttpContext();
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.GetProfile();

        // Assert
        result.ShouldNotBeNull();
        result.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task GetProfile_WithValidTokenButUserNotFound_ShouldReturnUnauthorized()
    {
        // Arrange
        _tokenExtractionServiceMock
            .Setup(x => x.ExtractTokenFromHeader(It.IsAny<string>()))
            .Returns("valid-token");

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetUserProfileQuery>(q => q.Token == "valid-token"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((UserDto?)null);

        // Setup HTTP context
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers.Authorization = "Bearer valid-token";
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.GetProfile();

        // Assert
        result.ShouldNotBeNull();
        result.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task ValidateToken_WithValidToken_ShouldReturnOkWithValidTrue()
    {
        // Arrange
        var request = new ValidateTokenRequest { Token = "valid-token" };
        var response = new ValidateTokenResponse { Valid = true };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<ValidateTokenQuery>(q => q.Token == "valid-token"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ValidateToken(request);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedResponse = okResult.Value.ShouldBeOfType<ValidateTokenResponse>();
        returnedResponse.Valid.ShouldBeTrue();
    }

    [Test]
    public async Task ValidateToken_WithInvalidToken_ShouldReturnOkWithValidFalse()
    {
        // Arrange
        var request = new ValidateTokenRequest { Token = "invalid-token" };
        var response = new ValidateTokenResponse { Valid = false };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<ValidateTokenQuery>(q => q.Token == "invalid-token"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(response);

        // Act
        var result = await _controller.ValidateToken(request);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedResponse = okResult.Value.ShouldBeOfType<ValidateTokenResponse>();
        returnedResponse.Valid.ShouldBeFalse();
    }
}
