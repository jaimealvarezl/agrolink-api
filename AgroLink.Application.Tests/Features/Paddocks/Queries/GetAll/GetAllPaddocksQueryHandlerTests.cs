using AgroLink.Application.Features.Paddocks.Queries.GetAll;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Queries.GetAll;

[TestFixture]
public class GetAllPaddocksQueryHandlerTests
{
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private GetAllPaddocksQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _handler = new GetAllPaddocksQueryHandler(
            _paddockRepositoryMock.Object,
            _farmRepositoryMock.Object
        );
    }

    [Test]
    public async Task Handle_ReturnsAllPaddocks()
    {
        // Arrange
        var query = new GetAllPaddocksQuery();
        var paddocks = new List<Paddock>
        {
            new Paddock
            {
                Id = 1,
                Name = "Paddock 1",
                FarmId = 1,
                CreatedAt = DateTime.UtcNow,
            },
            new Paddock
            {
                Id = 2,
                Name = "Paddock 2",
                FarmId = 1,
                CreatedAt = DateTime.UtcNow,
            },
        };
        var farm = new Farm { Id = 1, Name = "Test Farm" };

        _paddockRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(paddocks);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farm.Id)).ReturnsAsync(farm);

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
        _paddockRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Paddock>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
