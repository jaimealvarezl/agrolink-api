using AgroLink.Application.Features.Animals.Queries.GetAll;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetAll;

[TestFixture]
public class GetAllAnimalsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _handler = new GetAllAnimalsQueryHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _storageServiceMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private GetAllAnimalsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsAllAnimals()
    {
        // Arrange
        var userId = 1;
        var query = new GetAllAnimalsQuery(userId);
        var lot1 = new Lot { Id = 1, Name = "Lot 1" };
        var lot2 = new Lot { Id = 2, Name = "Lot 2" };
        var animals = new List<Animal>
        {
            new()
            {
                Id = 1,
                TagVisual = "A001",
                Cuia = "CUIA-A001",
                Name = "Animal 1",
                LotId = 1,
                Lot = lot1,
                CreatedAt = DateTime.UtcNow,
                LifeStatus = LifeStatus.Active,
                AnimalOwners = [],
                Photos = [],
            },
            new()
            {
                Id = 2,
                TagVisual = "A002",
                Cuia = "CUIA-A002",
                Name = "Animal 2",
                LotId = 2,
                Lot = lot2,
                CreatedAt = DateTime.UtcNow,
                LifeStatus = LifeStatus.Active,
                AnimalOwners = [],
                Photos = [],
            },
        };

        _animalRepositoryMock
            .Setup(r => r.GetAllByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(animals);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(1)).ReturnsAsync(lot1);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(2)).ReturnsAsync(lot2);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());
        _animalPhotoRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalPhoto>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().TagVisual.ShouldBe("A001");
        result.First().LotName.ShouldBe("Lot 1");
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoAnimalsExist()
    {
        // Arrange
        var userId = 1;
        var query = new GetAllAnimalsQuery(userId);
        _animalRepositoryMock
            .Setup(r => r.GetAllByUserAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Animal>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
