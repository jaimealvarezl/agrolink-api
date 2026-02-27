using AgroLink.Application.Features.Paddocks.Queries.GetAll;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Queries.GetAll;

[TestFixture]
public class GetAllPaddocksQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAllPaddocksQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAllPaddocksQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsAllPaddocks()
    {
        // Arrange
        var query = new GetAllPaddocksQuery();
        var paddocks = new List<Paddock>
        {
            new()
            {
                Id = 1,
                Name = "Paddock 1",
                FarmId = 1,
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                Name = "Paddock 2",
                FarmId = 1,
                CreatedAt = DateTime.UtcNow,
            },
        };
        var farm = new Farm { Id = 1, Name = "Test Farm" };

        _mocker.GetMock<IPaddockRepository>().Setup(r => r.GetAllAsync()).ReturnsAsync(paddocks);
        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farm.Id)).ReturnsAsync(farm);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().Name.ShouldBe("Paddock 1");
        result.First().FarmName.ShouldBe(farm.Name);
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoPaddocksExist()
    {
        // Arrange
        var query = new GetAllPaddocksQuery();
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Paddock>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
