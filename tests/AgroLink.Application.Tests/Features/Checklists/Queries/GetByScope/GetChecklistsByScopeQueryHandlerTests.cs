using System.Linq.Expressions;
using AgroLink.Application.Features.Checklists.Queries.GetByScope;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Queries.GetByScope;

[TestFixture]
public class GetChecklistsByLotQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetChecklistsByLotQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetChecklistsByLotQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingLotWithChecklists_ReturnsChecklistsDto()
    {
        // Arrange
        var lotId = 1;
        var farmId = 10;
        var query = new GetChecklistsByLotQuery(lotId);
        var checklists = new List<Checklist>
        {
            new()
            {
                Id = 1,
                LotId = lotId,
                Date = DateTime.Today,
                UserId = 1,
            },
            new()
            {
                Id = 2,
                LotId = lotId,
                Date = DateTime.Today.AddDays(-1),
                UserId = 1,
            },
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot
        {
            Id = lotId,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(lotId))
            .ReturnsAsync(lot);
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByLotIdAsync(lotId))
            .ReturnsAsync(checklists);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(new List<ChecklistItem>());
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Animal, bool>>>()))
            .ReturnsAsync(new List<Animal>());
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>()))
            .ReturnsAsync(new List<Lot>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().LotName.ShouldBe(lot.Name);
        result.First().UserName.ShouldBe(user.Name);
    }

    [Test]
    public async Task Handle_LotFromAnotherFarm_ReturnsEmptyList()
    {
        // Arrange
        var lotId = 1;
        var currentFarmId = 10;
        var lotFarmId = 20;
        var query = new GetChecklistsByLotQuery(lotId);
        var lot = new Lot
        {
            Id = lotId,
            Paddock = new Paddock { FarmId = lotFarmId },
        };

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(lotId))
            .ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_LotWithNoChecklists_ReturnsEmptyList()
    {
        // Arrange
        var lotId = 1;
        var query = new GetChecklistsByLotQuery(lotId);

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByLotIdAsync(lotId))
            .ReturnsAsync(new List<Checklist>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
