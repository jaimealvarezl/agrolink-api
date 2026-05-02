using AgroLink.Api.Controllers;
using AgroLink.Application.Features.Auth.Commands.UpdateProfile;
using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Features.Auth.Queries.GetUserProfile;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Api.Tests.Controllers;

[TestFixture]
public class AuthControllerTests
{
    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AuthController(_mediatorMock.Object);
    }

    private Mock<IMediator> _mediatorMock = null!;
    private AuthController _controller = null!;

    [Test]
    public async Task GetProfile_WhenUserExists_ReturnsOkWithUserDto()
    {
        var userDto = new UserDto
        {
            Id = 1,
            Name = "Test User",
            Email = "test@example.com",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(userDto);

        var result = await _controller.GetProfile();

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBeOfType<UserDto>().Name.ShouldBe("Test User");
    }

    [Test]
    public async Task GetProfile_WhenUserNotFound_ReturnsUnauthorized()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetUserProfileQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserDto?)null);

        var result = await _controller.GetProfile();

        result.Result.ShouldBeOfType<UnauthorizedResult>();
    }

    [Test]
    public async Task UpdateProfile_WithValidRequest_ReturnsOkWithUpdatedUser()
    {
        var request = new UpdateProfileRequest { Name = "Updated Name" };
        var userDto = new UserDto
        {
            Id = 1,
            Name = "Updated Name",
            Email = "test@example.com",
            Role = "USER",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<UpdateProfileCommand>(c => c.Request.Name == "Updated Name"),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(userDto);

        var result = await _controller.UpdateProfile(request);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBeOfType<UserDto>().Name.ShouldBe("Updated Name");
    }
}
