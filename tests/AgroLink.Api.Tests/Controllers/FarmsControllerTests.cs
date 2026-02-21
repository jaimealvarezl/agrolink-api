using System.Globalization;
using System.Security.Claims;
using AgroLink.Api.Controllers;
using AgroLink.Api.DTOs.Farms;
using AgroLink.Application.Features.Farms.Commands.Create;
using AgroLink.Application.Features.Farms.Commands.Delete;
using AgroLink.Application.Features.Farms.Commands.Update;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Application.Features.Farms.Queries.GetAll;
using AgroLink.Application.Features.Farms.Queries.GetById;
using AgroLink.Domain.Constants;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Api.Tests.Controllers;

[TestFixture]
public class FarmsControllerTests
{
    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new FarmsController(_mediatorMock.Object);
    }

    private Mock<IMediator> _mediatorMock = null!;
    private FarmsController _controller = null!;

    [Test]
    public async Task GetAll_ShouldReturnOkWithFarms()
    {
        // Arrange
        var farms = new List<FarmDto>
        {
            new()
            {
                Id = 1,
                Name = "Farm 1",
                OwnerId = 1,
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Name = "Farm 2",
                OwnerId = 1,
                Role = "Owner",
                CreatedAt = DateTime.UtcNow,
            },
        };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllFarmsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(farms);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedFarms = okResult.Value.ShouldBeOfType<List<FarmDto>>();
        returnedFarms.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetById_WhenFarmExists_ShouldReturnOk()
    {
        // Arrange
        var farm = new FarmDto
        {
            Id = 1,
            Name = "Farm 1",
            OwnerId = 1,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow,
        };
        _mediatorMock
            .Setup(x =>
                x.Send(It.Is<GetFarmByIdQuery>(q => q.Id == 1), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(farm);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedFarm = okResult.Value.ShouldBeOfType<FarmDto>();
        returnedFarm.Id.ShouldBe(1);
    }

    [Test]
    public async Task GetById_WhenFarmDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _mediatorMock
            .Setup(x =>
                x.Send(It.Is<GetFarmByIdQuery>(q => q.Id == 999), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((FarmDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Create_ShouldReturnCreated()
    {
        // Arrange
        var userId = 1;
        var request = new CreateFarmRequest { Name = "New Farm" };
        var farmDto = new FarmDto
        {
            Id = 1,
            Name = "New Farm",
            OwnerId = 5,
            Role = FarmMemberRoles.Owner,
            CreatedAt = DateTime.UtcNow,
        };

        // Mock Controller Context with User Claims
        var claims = new List<Claim>
        {
            new("userid", userId.ToString(CultureInfo.InvariantCulture)),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal },
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<CreateFarmCommand>(c => c.Name == "New Farm" && c.UserId == userId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(farmDto);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(farmDto);
    }

    [Test]
    public async Task Update_ShouldReturnOk()
    {
        // Arrange
        var request = new UpdateFarmRequest { Name = "Updated Farm" };
        var farmDto = new FarmDto
        {
            Id = 1,
            Name = "Updated Farm",
            OwnerId = 1,
            Role = "Owner",
            CreatedAt = DateTime.UtcNow,
        };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateFarmCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(farmDto);

        // Act
        var result = await _controller.Update(1, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(farmDto);
    }

    [Test]
    public async Task Delete_ShouldReturnNoContent()
    {
        // Arrange
        var userId = 1;

        // Mock Controller Context with User Claims
        var claims = new List<Claim>
        {
            new("userid", userId.ToString(CultureInfo.InvariantCulture)),
        };
        var identity = new ClaimsIdentity(claims, "TestAuthType");
        var claimsPrincipal = new ClaimsPrincipal(identity);
        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = claimsPrincipal },
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DeleteFarmCommand>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(1);

        // Assert
        result.ShouldBeOfType<NoContentResult>();
    }
}
