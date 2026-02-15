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
        _handler = new GetAnimalsByLotQueryHandler(
            _animalRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _animalPhotoRepositoryMock.Object,
            _storageServiceMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IAnimalPhotoRepository> _animalPhotoRepositoryMock = null!;
    private Mock<IStorageService> _storageServiceMock = null!;
    private GetAnimalsByLotQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingLotWithAnimals_ReturnsAnimalsDto()
    {
        // Arrange
        const int lotId = 1;
        const int userId = 1;
        var query = new GetAnimalsByLotQuery(lotId, userId);
        var lot = new Lot { Id = lotId, Name = "Test Lot" };
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
    public async Task Handle_ExistingLotWithNoAnimals_ReturnsEmptyList()
    {
        // Arrange
        const int lotId = 1;
        const int userId = 1;
        var query = new GetAnimalsByLotQuery(lotId, userId);

        _animalRepositoryMock
            .Setup(r => r.GetByLotIdAsync(lotId, userId))
            .ReturnsAsync(new List<Animal>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
