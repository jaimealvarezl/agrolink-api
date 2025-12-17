using AgroLink.Application.Features.Lots.Queries.GetAll;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Queries.GetAll;

[TestFixture]
public class GetAllLotsQueryHandlerTests
{
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private GetAllLotsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new GetAllLotsQueryHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object
        );
    }

    [Test]
    public async Task Handle_ReturnsAllLots()
    {
        // Arrange
        var query = new GetAllLotsQuery();
        var lots = new List<Lot>
        {
            new Lot
            {
                Id = 1,
                Name = "Lot 1",
                PaddockId = 1,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
            new Lot
            {
                Id = 2,
                Name = "Lot 2",
                PaddockId = 1,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
        };
        var paddock = new Paddock { Id = 1, Name = "Test Paddock" };

        _lotRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(lots);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddock.Id)).ReturnsAsync(paddock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().Name.ShouldBe("Lot 1");
        result.First().PaddockName.ShouldBe(paddock.Name);
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoLotsExist()
    {
        // Arrange
        var query = new GetAllLotsQuery();
        _lotRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Lot>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
