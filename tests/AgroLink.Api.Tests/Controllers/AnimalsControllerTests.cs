using System.Security.Claims;
using AgroLink.Api.Controllers;
using AgroLink.Application.Features.Animals.Commands.Create;
using AgroLink.Application.Features.Animals.Commands.Delete;
using AgroLink.Application.Features.Animals.Commands.Move;
using AgroLink.Application.Features.Animals.Commands.Update;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Queries.GetAll;
using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Features.Animals.Queries.GetByLot;
using AgroLink.Application.Features.Animals.Queries.GetGenealogy;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Api.Tests.Controllers;

[TestFixture]
public class AnimalsControllerTests
{
    private Mock<IMediator> _mediatorMock = null!;
    private AnimalsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _mediatorMock = new Mock<IMediator>();
        _controller = new AnimalsController(_mediatorMock.Object);

        // Setup HTTP context with user claims for tests that need it
        var claims = new List<Claim> { new("userid", "1"), new(ClaimTypes.Name, "testuser") };
        var identity = new ClaimsIdentity(claims, "test");
        var principal = new ClaimsPrincipal(identity);

        _controller.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = principal },
        };
    }

    [Test]
    public async Task GetAll_ShouldReturnOkWithAnimals()
    {
        // Arrange
        var animals = new List<AnimalDto>
        {
            new()
            {
                Id = 1,
                Tag = "A001",
                Name = "Animal 1",
                Color = "Brown",
                Breed = "Holstein",
                Sex = "Female",
                BirthDate = DateTime.UtcNow.AddYears(-2),
                LotId = 1,
                LotName = "Test Lot",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Tag = "A002",
                Name = "Animal 2",
                Color = "Black",
                Breed = "Angus",
                Sex = "Male",
                BirthDate = DateTime.UtcNow.AddYears(-1),
                LotId = 1,
                LotName = "Test Lot",
                CreatedAt = DateTime.UtcNow,
            },
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<GetAllAnimalsQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(animals);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimals = okResult.Value.ShouldBeOfType<List<AnimalDto>>();
        returnedAnimals.Count.ShouldBe(2);
    }

    [Test]
    public async Task GetById_WhenAnimalExists_ShouldReturnOkWithAnimal()
    {
        // Arrange
        var animalId = 1;
        var animal = new AnimalDto
        {
            Id = animalId,
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            LotName = "Test Lot",
            CreatedAt = DateTime.UtcNow,
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalByIdQuery>(q => q.Id == animalId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(animal);

        // Act
        var result = await _controller.GetById(animalId);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimal = okResult.Value.ShouldBeOfType<AnimalDto>();
        returnedAnimal.Id.ShouldBe(animalId);
        returnedAnimal.Tag.ShouldBe("A001");
    }

    [Test]
    public async Task GetById_WhenAnimalDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var animalId = 999;

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalByIdQuery>(q => q.Id == animalId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((AnimalDto?)null);

        // Act
        var result = await _controller.GetById(animalId);

        // Assert
        result.ShouldNotBeNull();
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task GetByLot_ShouldReturnOkWithAnimals()
    {
        // Arrange
        var lotId = 1;
        var animals = new List<AnimalDto>
        {
            new()
            {
                Id = 1,
                Tag = "A001",
                Name = "Animal 1",
                Color = "Brown",
                Breed = "Holstein",
                Sex = "Female",
                BirthDate = DateTime.UtcNow.AddYears(-2),
                LotId = lotId,
                LotName = "Test Lot",
                CreatedAt = DateTime.UtcNow,
            },
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalsByLotQuery>(q => q.LotId == lotId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(animals);

        // Act
        var result = await _controller.GetByLot(lotId);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimals = okResult.Value.ShouldBeOfType<List<AnimalDto>>();
        returnedAnimals.Count.ShouldBe(1);
        returnedAnimals.First().LotId.ShouldBe(lotId);
    }

    [Test]
    public async Task Create_WithValidData_ShouldReturnCreatedAtAction()
    {
        // Arrange
        var createDto = new CreateAnimalDto
        {
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            Owners = new List<AnimalOwnerDto>
            {
                new() { OwnerId = 1, SharePercent = 100 },
            },
        };

        var createdAnimal = new AnimalDto
        {
            Id = 1,
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            LotName = "Test Lot",
            CreatedAt = DateTime.UtcNow,
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<CreateAnimalCommand>(c => c.Dto == createDto),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(createdAnimal);

        // Act
        var result = await _controller.Create(createDto);

        // Assert
        result.ShouldNotBeNull();
        var createdAtActionResult = result.Result.ShouldBeOfType<CreatedAtActionResult>();
        var returnedAnimal = createdAtActionResult.Value.ShouldBeOfType<AnimalDto>();
        returnedAnimal.Id.ShouldBe(1);
        returnedAnimal.Tag.ShouldBe("A001");
    }

    [Test]
    public async Task Update_WhenAnimalExists_ShouldReturnOkWithUpdatedAnimal()
    {
        // Arrange
        var animalId = 1;
        var updateDto = new UpdateAnimalDto
        {
            Name = "Updated Animal",
            Color = "Black",
            Breed = "Angus",
            Status = "Active",
            BirthDate = DateTime.UtcNow.AddYears(-3),
            Owners = new List<AnimalOwnerDto>(),
        };

        var updatedAnimal = new AnimalDto
        {
            Id = animalId,
            Tag = "A001",
            Name = "Updated Animal",
            Color = "Black",
            Breed = "Angus",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-3),
            LotId = 1,
            LotName = "Test Lot",
            CreatedAt = DateTime.UtcNow,
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<UpdateAnimalCommand>(c => c.Id == animalId && c.Dto == updateDto),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(updatedAnimal);

        // Act
        var result = await _controller.Update(animalId, updateDto);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimal = okResult.Value.ShouldBeOfType<AnimalDto>();
        returnedAnimal.Id.ShouldBe(animalId);
        returnedAnimal.Name.ShouldBe("Updated Animal");
    }

    [Test]
    public async Task Update_WhenAnimalDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var animalId = 999;
        var updateDto = new UpdateAnimalDto
        {
            Name = "Updated Animal",
            Owners = new List<AnimalOwnerDto>(),
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<UpdateAnimalCommand>(c => c.Id == animalId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ArgumentException("Animal not found"));

        // Act
        var result = await _controller.Update(animalId, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.Result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task Delete_WhenAnimalExists_ShouldReturnNoContent()
    {
        // Arrange
        var animalId = 1;

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<DeleteAnimalCommand>(c => c.Id == animalId),
                    It.IsAny<CancellationToken>()
                )
            )
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.Delete(animalId);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<NoContentResult>();
    }

    [Test]
    public async Task Delete_WhenAnimalDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var animalId = 999;

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<DeleteAnimalCommand>(c => c.Id == animalId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ThrowsAsync(new ArgumentException("Animal not found"));

        // Act
        var result = await _controller.Delete(animalId);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<BadRequestObjectResult>();
    }

    [Test]
    public async Task GetGenealogy_WhenAnimalExists_ShouldReturnOkWithGenealogy()
    {
        // Arrange
        var animalId = 1;
        var genealogy = new AnimalGenealogyDto
        {
            Id = animalId,
            Tag = "A001",
            Name = "Test Animal",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalGenealogyQuery>(q => q.Id == animalId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(genealogy);

        // Act
        var result = await _controller.GetGenealogy(animalId);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedGenealogy = okResult.Value.ShouldBeOfType<AnimalGenealogyDto>();
        returnedGenealogy.Id.ShouldBe(animalId);
        returnedGenealogy.Tag.ShouldBe("A001");
    }

    [Test]
    public async Task GetGenealogy_WhenAnimalDoesNotExist_ShouldReturnNotFound()
    {
        // Arrange
        var animalId = 999;

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<GetAnimalGenealogyQuery>(q => q.Id == animalId),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync((AnimalGenealogyDto?)null);

        // Act
        var result = await _controller.GetGenealogy(animalId);

        // Assert
        result.ShouldNotBeNull();
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task MoveAnimal_WithValidData_ShouldReturnOkWithMovedAnimal()
    {
        // Arrange
        var animalId = 1;
        var moveRequest = new MoveAnimalRequest
        {
            FromLotId = 1,
            ToLotId = 2,
            Reason = "Health check",
        };

        var movedAnimal = new AnimalDto
        {
            Id = animalId,
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 2, // Moved to new lot
            LotName = "New Lot",
            CreatedAt = DateTime.UtcNow,
        };

        _mediatorMock
            .Setup(x =>
                x.Send(
                    It.Is<MoveAnimalCommand>(c =>
                        c.AnimalId == animalId
                        && c.FromLotId == moveRequest.FromLotId
                        && c.ToLotId == moveRequest.ToLotId
                    ),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(movedAnimal);

        // Act
        var result = await _controller.MoveAnimal(animalId, moveRequest);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimal = okResult.Value.ShouldBeOfType<AnimalDto>();
        returnedAnimal.Id.ShouldBe(animalId);
        returnedAnimal.LotId.ShouldBe(2);
    }
}
