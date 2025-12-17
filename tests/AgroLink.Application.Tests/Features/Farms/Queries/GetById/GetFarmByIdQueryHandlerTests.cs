using AgroLink.Application.Features.Farms.Queries.GetById;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Queries.GetById;

[TestFixture]
public class GetFarmByIdQueryHandlerTests
{
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private GetFarmByIdQueryHandler _handler = null!;

    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _handler = new GetFarmByIdQueryHandler(_farmRepositoryMock.Object);
    }

    [Test]
    public async Task Handle_ExistingFarm_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var query = new GetFarmByIdQuery(farmId);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Test Farm",
            Location = "Test Location",
            CreatedAt = DateTime.UtcNow,
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(farm.Name);
    }

    [Test]
    public async Task Handle_NonExistingFarm_ReturnsNull()
    {
        // Arrange
        var farmId = 999;
        var query = new GetFarmByIdQuery(farmId);

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync((Farm?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
