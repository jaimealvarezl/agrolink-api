using AgroLink.Application.Features.Lots.Queries.GetAll;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Queries.GetAll;

[TestFixture]
public class GetAllLotsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAllLotsQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAllLotsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsAllLots()
    {
        // Arrange
        var query = new GetAllLotsQuery();
        var lots = new List<Lot>
        {
            new()
            {
                Id = 1,
                Name = "Lot 1",
                PaddockId = 1,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Name = "Lot 2",
                PaddockId = 1,
                Status = "ACTIVE",
                CreatedAt = DateTime.UtcNow,
            },
        };
        var paddock = new Paddock { Id = 1, Name = "Test Paddock" };

        _mocker.GetMock<ILotRepository>().Setup(r => r.GetAllAsync()).ReturnsAsync(lots);
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetByIdAsync(paddock.Id))
            .ReturnsAsync(paddock);

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
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Lot>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
