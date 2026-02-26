using AgroLink.Application.Features.Animals.Queries.GetByLot;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetByLot;

[TestFixture]
public class GetAnimalsByLotQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalsByLotQueryHandler>();
    }

    private AutoMocker _mocker = null!;
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

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByLotIdAsync(lotId, userId))
            .ReturnsAsync(animals);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(lot.PaddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker
            .GetMock<IAnimalOwnerRepository>()
            .Setup(r => r.GetByAnimalIdAsync(It.IsAny<int>()))
            .ReturnsAsync(new List<AnimalOwner>());
        _mocker
            .GetMock<IAnimalPhotoRepository>()
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

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(lot.PaddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

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

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByLotIdAsync(lotId, userId))
            .ReturnsAsync(new List<Animal>());
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(lot.PaddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
