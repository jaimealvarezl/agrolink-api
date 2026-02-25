using AgroLink.Application.Features.Animals.Queries.GetByLot;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetByLot;

[TestFixture]
public class GetAnimalsByLotQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _animalPhotoRepositoryMock = new Mock<IAnimalPhotoRepository>();
        _storageServiceMock = new Mock<IStorageService>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new GetAnimalsByLotQueryHandler(
            _animalRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _storageServiceMock.Object,
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private GetAnimalsByLotQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingLotWithAnimals_ReturnsAnimalsDto()
    {
        // Arrange
        const int lotId = 1;
        const int farmId = 10;
        const int userId = 1;
        var query = new GetAnimalsByLotQuery(lotId, userId);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = 1,
        };
        var paddock = new Paddock { Id = 1, FarmId = farmId };
        var animals = new List<Animal>
        {
            new()
            {
                Id = 1,
                TagVisual = "A001",
                Cuia = "CUIA-A001",
                Name = "Animal 1",
                LotId = lotId,
                CreatedAt = DateTime.UtcNow,
                LifeStatus = LifeStatus.Active,
                Lot = lot,
            },
            new()
            {
                Id = 2,
                TagVisual = "A002",
                Cuia = "CUIA-A002",
                Name = "Animal 2",
                LotId = lotId,
                CreatedAt = DateTime.UtcNow,
                LifeStatus = LifeStatus.Active,
                Lot = lot,
            },
        };

        _animalRepositoryMock.Setup(r => r.GetByLotIdAsync(lotId, userId)).ReturnsAsync(animals);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(lot.PaddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);
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
        result.First().LotName.ShouldBe("Test Lot");
    }

    [Test]
    public async Task Handle_LotFromAnotherFarm_ReturnsEmptyList()
    {
        // Arrange
        const int lotId = 1;
        const int currentFarmId = 10;
        const int lotFarmId = 20;
        const int userId = 1;
        var query = new GetAnimalsByLotQuery(lotId, userId);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = 1,
        };
        var paddock = new Paddock { Id = 1, FarmId = lotFarmId };

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(lot.PaddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ExistingLotWithNoAnimals_ReturnsEmptyList()
    {
        // Arrange
        const int lotId = 1;
        const int farmId = 10;
        const int userId = 1;
        var query = new GetAnimalsByLotQuery(lotId, userId);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = 1,
        };
        var paddock = new Paddock { Id = 1, FarmId = farmId };

        _animalRepositoryMock
            .Setup(r => r.GetByLotIdAsync(lotId, userId))
            .ReturnsAsync(new List<Animal>());
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(lot.PaddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
