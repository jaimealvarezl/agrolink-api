using AgroLink.Application.Features.Lots.Queries.GetByPaddock;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Queries.GetByPaddock;

[TestFixture]
public class GetLotsByPaddockQueryHandlerTests
{
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private GetLotsByPaddockQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new GetLotsByPaddockQueryHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object
        );
    }

    [Test]
    public async Task Handle_ExistingPaddockWithLots_ReturnsLotsDto()
    {
        // Arrange
        var paddockId = 1;
        var query = new GetLotsByPaddockQuery(paddockId);
        var lots = new List<Lot>
        {
            new Lot
            {
                Id = 1,
                Name = "Lot 1",
                PaddockId = paddockId,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
            new Lot
            {
                Id = 2,
                Name = "Lot 2",
                PaddockId = paddockId,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
        };
        var paddock = new Paddock { Id = paddockId, Name = "Test Paddock" };

        _lotRepositoryMock.Setup(r => r.GetByPaddockIdAsync(paddockId)).ReturnsAsync(lots);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.All(l => l.PaddockId == paddockId).ShouldBeTrue();
        result.First().PaddockName.ShouldBe(paddock.Name);
    }

    [Test]
    public async Task Handle_ExistingPaddockWithNoLots_ReturnsEmptyList()
    {
        // Arrange
        var paddockId = 1;
        var query = new GetLotsByPaddockQuery(paddockId);
        var paddock = new Paddock { Id = paddockId, Name = "Test Paddock" };

        _lotRepositoryMock
            .Setup(r => r.GetByPaddockIdAsync(paddockId))
            .ReturnsAsync(new List<Lot>());
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
