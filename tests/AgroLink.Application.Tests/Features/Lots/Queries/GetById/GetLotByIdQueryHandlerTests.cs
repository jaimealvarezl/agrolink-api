using AgroLink.Application.Features.Lots.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Queries.GetById;

[TestFixture]
public class GetLotByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetLotByIdQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetLotByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingLot_ReturnsLotDto()
    {
        // Arrange
        const int lotId = 1;
        const int farmId = 10;
        var query = new GetLotByIdQuery(lotId);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = 1,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
        };
        var paddock = new Paddock
        {
            Id = 1,
            Name = "Test Paddock",
            FarmId = farmId,
        };

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
        result.Id.ShouldBe(lotId);
        result.Name.ShouldBe(lot.Name);
        result.PaddockName.ShouldBe(paddock.Name);
    }

    [Test]
    public async Task Handle_LotFromAnotherFarm_ReturnsNull()
    {
        // Arrange
        const int lotId = 1;
        const int currentFarmId = 10;
        const int lotFarmId = 20;
        var query = new GetLotByIdQuery(lotId);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = 1,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
        };
        var paddock = new Paddock
        {
            Id = 1,
            Name = "Test Paddock",
            FarmId = lotFarmId,
        };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(lot.PaddockId))
            .ReturnsAsync(paddock);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NonExistingLot_ReturnsNull()
    {
        // Arrange
        const int lotId = 999;
        var query = new GetLotByIdQuery(lotId);

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetByIdAsync(lotId))
            .ReturnsAsync((Lot?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
