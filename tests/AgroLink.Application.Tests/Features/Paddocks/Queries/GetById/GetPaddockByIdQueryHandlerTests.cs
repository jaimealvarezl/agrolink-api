using AgroLink.Application.Features.Paddocks.Queries.GetById;
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
        _handler = new GetPaddockByIdQueryHandler(
            _paddockRepositoryMock.Object,
            _farmRepositoryMock.Object
        );
    }

    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private GetPaddockByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingPaddock_ReturnsPaddockDto()
    {
        // Arrange
        var paddockId = 1;
        var query = new GetPaddockByIdQuery(paddockId);
        var paddock = new Paddock
        {
            Id = paddockId,
            Name = "Test Paddock",
            FarmId = 1,
            CreatedAt = DateTime.UtcNow,
        };
        var farm = new Farm { Id = 1, Name = "Test Farm" };

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync(paddock);
        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farm.Id)).ReturnsAsync(farm);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(paddockId);
        result.Name.ShouldBe(paddock.Name);
        result.FarmName.ShouldBe(farm.Name);
    }

    [Test]
    public async Task Handle_NonExistingPaddock_ReturnsNull()
    {
        // Arrange
        var paddockId = 999;
        var query = new GetPaddockByIdQuery(paddockId);

        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(paddockId)).ReturnsAsync((Paddock?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
