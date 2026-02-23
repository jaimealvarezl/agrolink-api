using AgroLink.Application.Features.Animals.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetById;

[TestFixture]
public class GetAnimalByIdQueryHandlerTests
{
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private GetAnimalByIdQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();

        _handler = new GetAnimalByIdQueryHandler(
            _animalRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _storageServiceMock.Object,
            _currentUserServiceMock.Object
        );
    }

    [Test]
    public async Task Handle_ExistingAnimal_ReturnsAnimalDto()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        const int farmId = 100;
        var query = new GetAnimalByIdQuery(animalId, userId);
        var animal = new Animal
        {
            Id = animalId,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Animal 1",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
            CreatedAt = DateTime.UtcNow,
            LifeStatus = LifeStatus.Active,
            Lot = new Lot 
            { 
                Name = "Test Lot",
                Paddock = new Paddock { FarmId = farmId }
            },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());
        _animalPhotoRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalPhoto>());
            
        // Setup CurrentUserService to match FarmId (or be null for backward compatibility if logic allows)
        // Here we explicitly match it to ensure robust test
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(animalId);
        result.TagVisual.ShouldBe(animal.TagVisual);
        result.LotName.ShouldBe("Test Lot");
    }

    [Test]
    public async Task Handle_NonExistingAnimal_ReturnsNull()
    {
        // Arrange
        const int animalId = 999;
        const int userId = 1;
        var query = new GetAnimalByIdQuery(animalId, userId);

        _animalRepositoryMock
            .Setup(r => r.GetByIdAsync(animalId, userId))
            .ReturnsAsync((Animal?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_WhenFarmIdDoesNotMatch_ReturnsNull()
    {
        // Arrange
        const int animalId = 1;
        const int userId = 1;
        const int animalFarmId = 100;
        const int userContextFarmId = 200; // Different Farm

        var query = new GetAnimalByIdQuery(animalId, userId);
        var animal = new Animal
        {
            Id = animalId,
            LotId = 1,
            Lot = new Lot 
            { 
                Name = "Test Lot",
                Paddock = new Paddock { FarmId = animalFarmId }
            },
        };

        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animalId, userId)).ReturnsAsync(animal);
        
        // Setup CurrentUserService to return a DIFFERENT FarmId
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(userContextFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull(); // Should filter out because of farm mismatch
    }
}
