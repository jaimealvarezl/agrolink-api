using AgroLink.Application.Features.Lots.Queries.GetById;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Lots.Queries.GetById;

[TestFixture]
public class GetLotByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new GetLotByIdQueryHandler(
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object
        );
    }

    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private GetLotByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingLot_ReturnsLotDto()
    {
        // Arrange
        var lotId = 1;
        var query = new GetLotByIdQuery(lotId);
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            PaddockId = 1,
            Status = "ACTIVE",
            CreatedAt = DateTime.UtcNow,
        };
        var paddock = new Paddock { Id = 1, Name = "Test Paddock" };

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync(lot);
        _paddockRepositoryMock.Setup(r => r.GetByIdAsync(lot.PaddockId)).ReturnsAsync(paddock);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(lotId);
        result.Name.ShouldBe(lot.Name);
        result.PaddockName.ShouldBe(paddock.Name);
    }

    [Test]
    public async Task Handle_NonExistingLot_ReturnsNull()
    {
        // Arrange
        var lotId = 999;
        var query = new GetLotByIdQuery(lotId);

        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lotId)).ReturnsAsync((Lot?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
