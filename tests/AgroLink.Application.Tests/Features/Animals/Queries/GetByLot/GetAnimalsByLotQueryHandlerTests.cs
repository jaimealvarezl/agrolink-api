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
        _lotRepositoryMock = new Mock<ILotRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _animalOwnerRepositoryMock = new Mock<IAnimalOwnerRepository>();
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _handler = new GetAnimalsByLotQueryHandler(
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _ownerRepositoryMock.Object,
            _animalOwnerRepositoryMock.Object,
            _photoRepositoryMock.Object
        );
    }

    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private Mock<IAnimalOwnerRepository> _animalOwnerRepositoryMock = null!;
    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private GetAnimalsByLotQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingLotWithAnimals_ReturnsAnimalsDto()
    {
        // Arrange
        var lotId = 1;
        var query = new GetAnimalsByLotQuery(lotId);
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
            },
        };
        var lot = new Lot { Id = lotId, Name = "Test Lot" };

        _animalRepositoryMock.Setup(r => r.GetByLotIdAsync(lotId)).ReturnsAsync(animals);
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _animalOwnerRepositoryMock
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());
        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(a => a.LotId == lotId).ShouldBeTrue();
        result.First().LotName.ShouldBe(lot.Name);
    }

    [Test]
    public async Task Handle_ExistingLotWithNoAnimals_ReturnsEmptyList()
    {
        // Arrange
        var lotId = 1;
        var query = new GetAnimalsByLotQuery(lotId);
        var lot = new Lot { Id = lotId, Name = "Test Lot" };

        _animalRepositoryMock.Setup(r => r.GetByLotIdAsync(lotId)).ReturnsAsync(new List<Animal>());
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
