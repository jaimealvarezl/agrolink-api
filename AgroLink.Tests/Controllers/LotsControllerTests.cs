using System.Security.Claims;
using AgroLink.Api.Controllers;
using AgroLink.Application.DTOs;
using AgroLink.Application.Features.Lots.Commands.Create;
using AgroLink.Application.Features.Lots.Commands.Move;
using AgroLink.Application.Features.Lots.Queries.GetAll;
using AgroLink.Application.Features.Lots.Queries.GetById;
using AgroLink.Application.Features.Lots.Queries.GetByPaddock;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Tests.Controllers;

[TestFixture]
public class LotsControllerTests
{
    private Mock<IMediator> _mediatorMock = null!;
    private LotsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new LotsController(_mediatorMock.Object);

        // Setup HTTP context for MoveLot (user id)
        var claims = new List<Claim> { new("userid", "1"), new(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    [Test]
    public async Task GetAll_ShouldReturnOk()
    {
        // Arrange
        var lots = new List<LotDto>
        {
            new() { Id = 1, Name = "Lot 1" },
        };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllLotsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lots);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedLots = okResult.Value.ShouldBeOfType<List<LotDto>>();
        returnedLots.Count.ShouldBe(1);
    }

    [Test]
    public async Task GetById_WhenLotExists_ShouldReturnOk()
    {
        // Arrange
        var lot = new LotDto { Id = 1, Name = "Lot 1" };
        _mediatorMock
            .Setup(x =>
                x.Send(It.Is<GetLotByIdQuery>(q => q.Id == 1), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(lot);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(lot);
    }

    [Test]
    public async Task GetById_WhenLotDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        _mediatorMock
            .Setup(x =>
                x.Send(It.Is<GetLotByIdQuery>(q => q.Id == 999), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync((LotDto?)null);

        // Act
        var result = await _controller.GetById(999);

        // Assert
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Create_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreateLotRequest { Name = "New Lot", PaddockId = 1 };
        var lotDto = new LotDto { Id = 1, Name = "New Lot" };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreateLotCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lotDto);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(lotDto);
    }

    [Test]
    public async Task MoveLot_ShouldReturnOk()
    {
        // Arrange
        var request = new MoveLotRequest { ToPaddockId = 2, Reason = "Rotation" };
        var lotDto = new LotDto
        {
            Id = 1,
            Name = "Lot 1",
            PaddockId = 2,
        };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<MoveLotCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(lotDto);

        // Act
        var result = await _controller.MoveLot(1, request);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(lotDto);
    }

    [Test]
    public async Task GetByPaddock_ShouldReturnOk()
    {
        // Arrange
        var lots = new List<LotDto>
        {
            new()
            {
                Id = 1,
                Name = "Lot 1",
                PaddockId = 1,
            },
        };
        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetLotsByPaddockQuery>(q => q.PaddockId == 1),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(lots);

        // Act
        var result = await _controller.GetByPaddock(1);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedLots = okResult.Value.ShouldBeOfType<List<LotDto>>();
        returnedLots.Count.ShouldBe(1);
    }
}
