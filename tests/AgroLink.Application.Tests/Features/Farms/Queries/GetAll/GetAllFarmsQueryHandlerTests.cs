using AgroLink.Application.Features.Farms.Queries.GetAll;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Queries.GetAll;

[TestFixture]
public class GetAllFarmsQueryHandlerTests
{
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private GetAllFarmsQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _handler = new GetAllFarmsQueryHandler(_farmRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ReturnsAllFarms()
    {
        // Arrange
        var query = new GetAllFarmsQuery();
        var farms = new List<Farm>
        {
            new Farm
            {
                Id = 1,
                Name = "Farm 1",
                Location = "Location 1",
                CreatedAt = DateTime.UtcNow,
            },
            new Farm
            {
                Id = 2,
                Name = "Farm 2",
                Location = "Location 2",
                CreatedAt = DateTime.UtcNow,
            },
        };

        _farmRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(farms);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().Name.ShouldBe("Farm 1");
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoFarmsExist()
    {
        // Arrange
        var query = new GetAllFarmsQuery();
        _farmRepositoryMock.Setup(r => r.GetAllAsync()).ReturnsAsync(new List<Farm>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
