using AgroLink.Api.Controllers;
using AgroLink.Application.Features.Paddocks.Commands.Create;
using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Application.Features.Paddocks.Queries.GetAll;
using AgroLink.Application.Features.Paddocks.Queries.GetById;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Api.Tests.Controllers;

[TestFixture]
public class PaddocksControllerTests
{
    private Mock<IMediator> _mediatorMock = null!;
    private PaddocksController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new PaddocksController(_mediatorMock.Object);
    }

    [Test]
    public async Task GetAll_ShouldReturnOk()
    {
        // Arrange
        var paddocks = new List<PaddockDto>
        {
            new() { Id = 1, Name = "Paddock 1" },
        };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllPaddocksQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paddocks);

        // Act
        var result = await _controller.GetAll();

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedPaddocks = okResult.Value.ShouldBeOfType<List<PaddockDto>>();
        returnedPaddocks.Count.ShouldBe(1);
    }

    [Test]
    public async Task GetById_WhenPaddockExists_ShouldReturnOk()
    {
        // Arrange
        var paddock = new PaddockDto { Id = 1, Name = "Paddock 1" };
        _mediatorMock
            .Setup(x =>
                x.Send(It.Is<GetPaddockByIdQuery>(q => q.Id == 1), It.IsAny<CancellationToken>())
            )
            .ReturnsAsync(paddock);

        // Act
        var result = await _controller.GetById(1);

        // Assert
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        okResult.Value.ShouldBe(paddock);
    }

    [Test]
    public async Task Create_ShouldReturnCreated()
    {
        // Arrange
        var request = new CreatePaddockRequest { Name = "New Paddock", FarmId = 1 };
        var paddockDto = new PaddockDto { Id = 1, Name = "New Paddock" };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CreatePaddockCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(paddockDto);

        // Act
        var result = await _controller.Create(request);

        // Assert
        var createdResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        createdResult.Value.ShouldBe(paddockDto);
    }
}
