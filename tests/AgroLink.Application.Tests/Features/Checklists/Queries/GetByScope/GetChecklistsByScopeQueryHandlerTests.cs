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
public class GetChecklistsByScopeQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetChecklistsByScopeQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetChecklistsByScopeQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingScopeWithChecklists_ReturnsChecklistsDto()
    {
        // Arrange
        var scopeType = "LOT";
        var scopeId = 1;
        var farmId = 10;
        var query = new GetChecklistsByScopeQuery(scopeType, scopeId);
        var checklists = new List<Checklist>
        {
            new()
            {
                Id = 1,
                ScopeType = scopeType,
                ScopeId = scopeId,
                Date = DateTime.Today,
                UserId = 1,
            },
            new()
            {
                Id = 2,
                ScopeType = scopeType,
                ScopeId = scopeId,
                Date = DateTime.Today.AddDays(-1),
                UserId = 1,
            },
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot
        {
            Id = scopeId,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByScopeAsync(scopeType, scopeId))
            .ReturnsAsync(checklists);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(new List<ChecklistItem>());
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(lot.Id))
            .ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().ScopeName.ShouldBe(lot.Name);
        result.First().UserName.ShouldBe(user.Name);
    }

    [Test]
    public async Task Handle_ScopeFromAnotherFarm_ReturnsEmptyList()
    {
        // Arrange
        var scopeType = "LOT";
        var scopeId = 1;
        var currentFarmId = 10;
        var scopeFarmId = 20;
        var query = new GetChecklistsByScopeQuery(scopeType, scopeId);
        var lot = new Lot
        {
            Id = scopeId,
            Paddock = new Paddock { FarmId = scopeFarmId },
        };

        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(scopeId))
            .ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_ExistingScopeWithNoChecklists_ReturnsEmptyList()
    {
        // Arrange
        var scopeType = "LOT";
        var scopeId = 1;
        var query = new GetChecklistsByScopeQuery(scopeType, scopeId);

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByScopeAsync(scopeType, scopeId))
            .ReturnsAsync(new List<Checklist>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
