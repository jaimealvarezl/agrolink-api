using System.Security.Claims;
using AgroLink.Api.Controllers;
using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Application.Features.Animals.Commands.Update;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Queries.GetAll;
using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Features.Animals.Queries.GetByLot;
using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using AgroLink.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Api.Tests.Controllers;

[TestFixture]
public class AnimalsControllerTests
{
    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AnimalsController(_mediatorMock.Object);

        var claims = new List<Claim> { new("userid", "1"), new(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    private Mock<IMediator> _mediatorMock = null!;
    private AnimalsController _controller = null!;

    private static AnimalDto CreateTestAnimalDto(int id)
    {
        return new AnimalDto
        {
            Id = id,
            Name = "Animal " + id,
            Sex = Sex.Female,
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            Owners = [],
            Photos = [],
            CreatedAt = DateTime.UtcNow,
        };
    }

    [Test]
    public async Task GetAll_ShouldReturnOkWithAnimals()
    {
        var animals = new List<AnimalDto> { CreateTestAnimalDto(1) };
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllAnimalsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(animals);

        var result = await _controller.GetAll(CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimals = okResult.Value.ShouldBeOfType<List<AnimalDto>>();
        returnedAnimals.Count.ShouldBe(1);
    }

    [Test]
    public async Task GetById_WhenAnimalExists_ShouldReturnOkWithAnimal()
    {
        var animalId = 1;
        var animal = CreateTestAnimalDto(animalId);
        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalByIdQuery>(q => q.Id == animalId && q.UserId == 1),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(animal);

        var result = await _controller.GetById(animalId, CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimal = okResult.Value.ShouldBeOfType<AnimalDto>();
        returnedAnimal.Id.ShouldBe(animalId);
    }

    [Test]
    public async Task GetByLot_ShouldReturnOkWithAnimals()
    {
        var lotId = 1;
        var animals = new List<AnimalDto> { CreateTestAnimalDto(1) };
        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalsByLotQuery>(q => q.LotId == lotId && q.UserId == 1),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(animals);

        var result = await _controller.GetByLot(lotId, CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimals = okResult.Value.ShouldBeOfType<List<AnimalDto>>();
        returnedAnimals.Count.ShouldBe(1);
    }

    [Test]
    public async Task Create_WithValidData_ShouldReturnCreatedAtAction()
    {
        var createDto = new CreateAnimalDto
        {
            Name = "Test Animal",
            Sex = Sex.Female,
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            Owners = [new AnimalOwnerCreateDto { OwnerId = 1, SharePercent = 100 }],
        };

        var createdAnimal = CreateTestAnimalDto(1);
        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<CreateAnimalCommand>(c => c.Dto == createDto),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(createdAnimal);

        var result = await _controller.Create(createDto, CancellationToken.None);

        var createdAtActionResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var returnedAnimal = createdAtActionResult.Value.ShouldBeOfType<AnimalDto>();
        returnedAnimal.Id.ShouldBe(1);
    }

    [Test]
    public async Task Update_WhenAnimalExists_ShouldReturnOkWithUpdatedAnimal()
    {
        var animalId = 1;
        var updateDto = new UpdateAnimalDto { Name = "Updated Animal" };
        var updatedAnimal = CreateTestAnimalDto(animalId);
        updatedAnimal.Name = "Updated Animal";

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<UpdateAnimalCommand>(c =>
                        c.Id == animalId && c.Dto == updateDto && c.UserId == 1
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(updatedAnimal);

        var result = await _controller.Update(animalId, updateDto, CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimal = okResult.Value.ShouldBeOfType<AnimalDto>();
        returnedAnimal.Name.ShouldBe("Updated Animal");
    }

    [Test]
    public async Task Delete_WhenAnimalExists_ShouldReturnNoContent()
    {
        var animalId = 1;
        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<DeleteAnimalCommand>(c => c.Id == animalId && c.UserId == 1),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(animalId, CancellationToken.None);

        result.ShouldBeOfType<NoContentResult>();
    }

    [Test]
    public async Task GetGenealogy_WhenAnimalExists_ShouldReturnOkWithGenealogy()
    {
        var animalId = 1;
        var genealogy = new AnimalGenealogyDto
        {
            Id = animalId,
            Name = "Test Animal",
            Sex = Sex.Female,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            Children = [],
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalGenealogyQuery>(q => q.Id == animalId && q.UserId == 1),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(genealogy);

        var result = await _controller.GetGenealogy(animalId, CancellationToken.None);

        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedGenealogy = okResult.Value.ShouldBeOfType<AnimalGenealogyDto>();
        returnedGenealogy.Id.ShouldBe(animalId);
    }
}
