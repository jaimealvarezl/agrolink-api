using AgroLink.Application.Features.Paddocks.Queries.GetByFarm;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Queries.GetByFarm;

[TestFixture]
public class GetPaddocksByFarmQueryHandlerTests
{
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private GetPaddocksByFarmQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _handler = new GetPaddocksByFarmQueryHandler(
            _paddockRepositoryMock.Object,
            _farmRepositoryMock.Object
        );
    }

    [Test]
    public async Task Handle_ExistingFarmWithPaddocks_ReturnsPaddocksDto()
    {
        // Arrange
        var farmId = 1;
        var query = new GetPaddocksByFarmQuery(farmId);
        var paddocks = new List<Paddock>
        {
            new Paddock
            {
                Id = 1,
                Name = "Paddock 1",
                FarmId = farmId,
                CreatedAt = DateTime.UtcNow,
            },
            new Paddock
            {
                Id = 2,
                Name = "Paddock 2",
                FarmId = farmId,
                CreatedAt = DateTime.UtcNow,
            },
        };
        var farm = new Farm { Id = farmId, Name = "Test Farm" };

        _paddockRepositoryMock.Setup(r => r.GetByFarmIdAsync(farmId)).ReturnsAsync(paddocks);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(p => p.FarmId == farmId).ShouldBeTrue();
        result.First().FarmName.ShouldBe(farm.Name);
    }

    [Test]
    public async Task Handle_ExistingFarmWithNoPaddocks_ReturnsEmptyList()
    {
        // Arrange
        var farmId = 1;
        var query = new GetPaddocksByFarmQuery(farmId);
        var farm = new Farm { Id = farmId, Name = "Test Farm" };

        _paddockRepositoryMock
            .Setup(r => r.GetByFarmIdAsync(farmId))
            .ReturnsAsync(new List<Paddock>());
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
