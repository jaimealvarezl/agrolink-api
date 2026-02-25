using AgroLink.Application.Features.Paddocks.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Paddocks.Queries.GetById;

[TestFixture]
public class GetPaddockByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _currentUserServiceMock = new Mock<ICurrentUserService>();
        _handler = new GetPaddockByIdQueryHandler(
            _paddockRepositoryMock.Object,
            _farmRepositoryMock.Object,
            _currentUserServiceMock.Object
        );
    }

    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<ICurrentUserService> _currentUserServiceMock = null!;
    private GetPaddockByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingPaddock_ReturnsPaddockDto()
    {
        // Arrange
        const int paddockId = 1;
        const int farmId = 10;
        var query = new GetPaddockByIdQuery(paddockId);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test Paddock",
            FarmId = farmId,
            CreatedAt = DateTime.UtcNow,
        };
        var farm = new Farm { Id = farmId, Name = "Test Farm" };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(paddockId);
        result.Name.ShouldBe(paddock.Name);
        result.FarmName.ShouldBe(farm.Name);
    }

    [Test]
    public async Task Handle_PaddockFromAnotherFarm_ReturnsNull()
    {
        // Arrange
        const int paddockId = 1;
        const int currentFarmId = 10;
        const int paddockFarmId = 20;
        var query = new GetPaddockByIdQuery(paddockId);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test Paddock",
            FarmId = paddockFarmId,
            CreatedAt = DateTime.UtcNow,
        };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _currentUserServiceMock.Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NonExistingPaddock_ReturnsNull()
    {
        // Arrange
        const int paddockId = 999;
        var query = new GetPaddockByIdQuery(paddockId);

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync((Paddock?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
