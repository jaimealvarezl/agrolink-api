using AgroLink.Application.Features.Lots.Queries.GetByPaddock;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Queries.GetByPaddock;

[TestFixture]
public class GetLotsByPaddockQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new GetLotsByPaddockQueryHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private GetLotsByPaddockQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingPaddockWithLots_ReturnsLotsDto()
    {
        // Arrange
        const int paddockId = 1;
        const int farmId = 10;
        var query = new GetLotsByPaddockQuery(paddockId);
        var lots = new List<Lot>
        {
            new()
            {
                Id = 1,
                Name = "Lot 1",
                PaddockId = paddockId,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Name = "Lot 2",
                PaddockId = paddockId,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
        };
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test Paddock",
            FarmId = farmId,
        };

        _lotRepositoryMock.Setup(r => r.GetByPaddockIdAsync(paddockId)).ReturnsAsync(lots);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(l => l.PaddockId == paddockId).ShouldBeTrue();
        result.First().PaddockName.ShouldBe(paddock.Name);
    }

    [Test]
    public async Task Handle_PaddockFromAnotherFarm_ReturnsEmptyList()
    {
        // Arrange
        const int paddockId = 1;
        const int currentFarmId = 10;
        const int paddockFarmId = 20;
        var query = new GetLotsByPaddockQuery(paddockId);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test Paddock",
            FarmId = paddockFarmId,
        };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ExistingPaddockWithNoLots_ReturnsEmptyList()
    {
        // Arrange
        const int paddockId = 1;
        const int farmId = 10;
        var query = new GetLotsByPaddockQuery(paddockId);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test Paddock",
            FarmId = farmId,
        };

        _lotRepositoryMock
            .Setup(r => r.GetByPaddockIdAsync(paddockId))
            .ReturnsAsync(new List<Lot>());
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
