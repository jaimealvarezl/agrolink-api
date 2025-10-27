using AgroLink.API.Controllers;
using AgroLink.Core.DTOs;
using AgroLink.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Shouldly;

namespace AgroLink.Tests.Controllers;

[TestFixture]
public class AnimalsControllerTests
{
    private Mock<IAnimalService> _animalServiceMock = null!;
    private AnimalsController _controller = null!;

    [SetUp]
    public void Setup()
    {
        _animalServiceMock = new Mock<IAnimalService>();
        _controller = new AnimalsController(_animalServiceMock.Object);
    }

    [Test]
    public async Task GetAll_ShouldReturnOkWithAnimals()
    {
        // Arrange
        var animals = new List<AnimalDto>
        {
            new AnimalDto
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
            new AnimalDto
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

        _animalServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync(animals);

        // Act
        var result = await _controller.GetAll();

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimals = okResult.Value.ShouldBeOfType<IEnumerable<AnimalDto>>();
        returnedAnimals.Count().ShouldBe(2);
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

        _animalServiceMock.Setup(x => x.GetByIdAsync(animalId)).ReturnsAsync(animal);

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

        _animalServiceMock.Setup(x => x.GetByIdAsync(animalId)).ReturnsAsync((AnimalDto?)null);

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
            new AnimalDto
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

        _animalServiceMock.Setup(x => x.GetByLotAsync(lotId)).ReturnsAsync(animals);

        // Act
        var result = await _controller.GetByLot(lotId);

        // Assert
        result.ShouldNotBeNull();
        var okResult = result.Result.ShouldBeOfType<OkObjectResult>();
        var returnedAnimals = okResult.Value.ShouldBeOfType<IEnumerable<AnimalDto>>();
        returnedAnimals.Count().ShouldBe(1);
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
                new AnimalOwnerDto { OwnerId = 1, SharePercent = 100 },
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

        _animalServiceMock.Setup(x => x.CreateAsync(createDto)).ReturnsAsync(createdAnimal);

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

        _animalServiceMock
            .Setup(x => x.UpdateAsync(animalId, updateDto))
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

        _animalServiceMock
            .Setup(x => x.UpdateAsync(animalId, updateDto))
            .ThrowsAsync(new ArgumentException("Animal not found"));

        // Act
        var result = await _controller.Update(animalId, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.Result.ShouldBeOfType<NotFoundResult>();
    }

    [Test]
    public async Task Delete_WhenAnimalExists_ShouldReturnNoContent()
    {
        // Arrange
        var animalId = 1;

        _animalServiceMock.Setup(x => x.DeleteAsync(animalId)).Returns(Task.CompletedTask);

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

        _animalServiceMock
            .Setup(x => x.DeleteAsync(animalId))
            .ThrowsAsync(new ArgumentException("Animal not found"));

        // Act
        var result = await _controller.Delete(animalId);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeOfType<NotFoundResult>();
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

        _animalServiceMock.Setup(x => x.GetGenealogyAsync(animalId)).ReturnsAsync(genealogy);

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

        _animalServiceMock
            .Setup(x => x.GetGenealogyAsync(animalId))
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

        _animalServiceMock
            .Setup(x =>
                x.MoveAnimalAsync(
                    animalId,
                    moveRequest.FromLotId,
                    moveRequest.ToLotId,
                    moveRequest.Reason,
                    1
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
