using System.Linq.Expressions;
using AgroLink.Application.Features.Checklists.Queries.GetByScope;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Queries.GetByScope;

[TestFixture]
public class GetChecklistsByScopeQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _checklistRepositoryMock = new Mock<IChecklistRepository>();
        _checklistItemRepositoryMock = new Mock<IRepository<ChecklistItem>>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new GetChecklistsByScopeQueryHandler(
            _checklistRepositoryMock.Object,
            _checklistItemRepositoryMock.Object,
            _userRepositoryMock.Object,
            _animalRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object
        );
    }

    private Mock<IChecklistRepository> _checklistRepositoryMock = null!;
    private Mock<IRepository<ChecklistItem>> _checklistItemRepositoryMock = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private GetChecklistsByScopeQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingScopeWithChecklists_ReturnsChecklistsDto()
    {
        // Arrange
        var scopeType = "LOT";
        var scopeId = 1;
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
        var lot = new Lot { Id = scopeId, Name = "Test Lot" };

        _checklistRepositoryMock
            .Setup(r => r.GetByScopeAsync(scopeType, scopeId))
            .ReturnsAsync(checklists);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _checklistItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(new List<ChecklistItem>());
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().ScopeName.ShouldBe(lot.Name);
        result.First().UserName.ShouldBe(user.Name);
    }

    [Test]
    public async Task Handle_ExistingScopeWithNoChecklists_ReturnsEmptyList()
    {
        // Arrange
        var scopeType = "LOT";
        var scopeId = 1;
        var query = new GetChecklistsByScopeQuery(scopeType, scopeId);

        _checklistRepositoryMock
            .Setup(r => r.GetByScopeAsync(scopeType, scopeId))
            .ReturnsAsync(new List<Checklist>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
