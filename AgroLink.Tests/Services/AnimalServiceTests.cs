using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Services;
using Moq;
using Shouldly;

namespace AgroLink.Tests.Services;

[TestFixture]
public class AnimalServiceTests
{
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private AnimalService _service = null!;

    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();

        _service = new AnimalService(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _photoRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object
        );
    }

    [Test]
    public async Task GetByIdAsync_WhenAnimalExists_ShouldReturnAnimalDto()
    {
        // Arrange
        var animal = new Animal
        {
            Id = 1,
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            CreatedAt = DateTime.UtcNow,
        };

        var lot = new Lot { Id = 1, Name = "Test Lot" };

        _animalRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(animal);
        _lotRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(lot);
        _animalOwnerRepositoryMock
            .Setup(x => x.GetByAnimalIdAsync(1))
            .ReturnsAsync(new List<AnimalOwner>());
        _photoRepositoryMock
            .Setup(x => x.GetByEntityAsync("ANIMAL", 1))
            .ReturnsAsync(new List<Photo>());

        // Act
        var result = await _service.GetByIdAsync(1);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(1);
        result.Tag.ShouldBe("A001");
        result.Name.ShouldBe("Test Animal");
        result.LotName.ShouldBe("Test Lot");
    }

    [Test]
    public async Task GetByIdAsync_WhenAnimalDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        _animalRepositoryMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Animal?)null);

        // Act
        var result = await _service.GetByIdAsync(999);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task GetAllAsync_ShouldReturnAllAnimals()
    {
        // Arrange
        var animals = new List<Animal>
        {
            new Animal
            {
                Id = 1,
                Tag = "A001",
                Name = "Animal 1",
                Color = "Brown",
                Breed = "Holstein",
                Sex = "Female",
                BirthDate = DateTime.UtcNow.AddYears(-2),
                LotId = 1,
                CreatedAt = DateTime.UtcNow,
            },
            new Animal
            {
                Id = 2,
                Tag = "A002",
                Name = "Animal 2",
                Color = "Black",
                Breed = "Angus",
                Sex = "Male",
                BirthDate = DateTime.UtcNow.AddYears(-1),
                LotId = 1,
                CreatedAt = DateTime.UtcNow,
            },
        };

        _animalRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(animals);
        _lotRepositoryMock
            .Setup(x => x.GetByIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new Lot { Id = 1, Name = "Test Lot" });
        _animalOwnerRepositoryMock
            .Setup(x => x.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());
        _photoRepositoryMock
            .Setup(x => x.GetByEntityAsync("ANIMAL", It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());

        // Act
        var result = await _service.GetAllAsync();

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
    }

    [Test]
    public async Task GetByLotAsync_ShouldReturnAnimalsInLot()
    {
        // Arrange
        var animals = new List<Animal>
        {
            new Animal
            {
                Id = 1,
                Tag = "A001",
                Name = "Animal 1",
                Color = "Brown",
                Breed = "Holstein",
                Sex = "Female",
                BirthDate = DateTime.UtcNow.AddYears(-2),
                LotId = 1,
                CreatedAt = DateTime.UtcNow,
            },
        };

        _animalRepositoryMock.Setup(x => x.GetByLotIdAsync(1)).ReturnsAsync(animals);
        _lotRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(new Lot { Id = 1, Name = "Test Lot" });
        _animalOwnerRepositoryMock
            .Setup(x => x.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());
        _photoRepositoryMock
            .Setup(x => x.GetByEntityAsync("ANIMAL", It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());

        // Act
        var result = await _service.GetByLotAsync(1);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().LotId.ShouldBe(1);
    }

    [Test]
    public async Task CreateAsync_ShouldCreateAnimal()
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

        var createdAnimal = new Animal
        {
            Id = 1,
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            CreatedAt = DateTime.UtcNow,
        };

        _animalRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<Animal>()))
            .Returns(Task.CompletedTask);
        _animalRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _animalOwnerRepositoryMock
            .Setup(x => x.AddAsync(It.IsAny<AnimalOwner>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.CreateAsync(createDto);

        // Assert
        result.ShouldNotBeNull();
        result.Tag.ShouldBe("A001");
        result.Name.ShouldBe("Test Animal");

        _animalRepositoryMock.Verify(x => x.AddAsync(It.IsAny<Animal>()), Times.Once);
        _animalRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Exactly(2));
        _animalOwnerRepositoryMock.Verify(x => x.AddAsync(It.IsAny<AnimalOwner>()), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenAnimalExists_ShouldUpdateAnimal()
    {
        // Arrange
        var existingAnimal = new Animal
        {
            Id = 1,
            Tag = "A001",
            Name = "Original Name",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            CreatedAt = DateTime.UtcNow,
        };

        var updateDto = new UpdateAnimalDto
        {
            Name = "Updated Name",
            Color = "Black",
            Breed = "Angus",
            Status = "Active",
            BirthDate = DateTime.UtcNow.AddYears(-3),
            Owners = new List<AnimalOwnerDto>(),
        };

        _animalRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingAnimal);
        _animalRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);
        _animalOwnerRepositoryMock
            .Setup(x => x.RemoveByAnimalIdAsync(1))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _service.UpdateAsync(1, updateDto);

        // Assert
        result.ShouldNotBeNull();
        result.Name.ShouldBe("Updated Name");
        result.Color.ShouldBe("Black");
        result.Breed.ShouldBe("Angus");

        _animalRepositoryMock.Verify(x => x.Update(It.IsAny<Animal>()), Times.Once);
        _animalRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task UpdateAsync_WhenAnimalDoesNotExist_ShouldThrowArgumentException()
    {
        // Arrange
        var updateDto = new UpdateAnimalDto
        {
            Name = "Updated Name",
            Owners = new List<AnimalOwnerDto>(),
        };

        _animalRepositoryMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Animal?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _service.UpdateAsync(999, updateDto));
    }

    [Test]
    public async Task DeleteAsync_WhenAnimalExists_ShouldDeleteAnimal()
    {
        // Arrange
        var existingAnimal = new Animal
        {
            Id = 1,
            Tag = "A001",
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            CreatedAt = DateTime.UtcNow,
        };

        _animalRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(existingAnimal);
        _animalRepositoryMock.Setup(x => x.SaveChangesAsync()).ReturnsAsync(1);

        // Act
        await _service.DeleteAsync(1);

        // Assert
        _animalRepositoryMock.Verify(x => x.Remove(It.IsAny<Animal>()), Times.Once);
        _animalRepositoryMock.Verify(x => x.SaveChangesAsync(), Times.Once);
    }

    [Test]
    public async Task DeleteAsync_WhenAnimalDoesNotExist_ShouldThrowArgumentException()
    {
        // Arrange
        _animalRepositoryMock.Setup(x => x.GetByIdAsync(999)).ReturnsAsync((Animal?)null);

        // Act & Assert
        await Should.ThrowAsync<ArgumentException>(() => _service.DeleteAsync(999));
    }
}
