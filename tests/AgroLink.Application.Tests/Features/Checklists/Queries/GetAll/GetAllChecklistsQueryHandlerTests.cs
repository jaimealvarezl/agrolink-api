using System.Linq.Expressions;
using AgroLink.Application.Features.Checklists.Queries.GetAll;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Queries.GetAll;

[TestFixture]
public class GetAllChecklistsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAllChecklistsQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAllChecklistsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsAllChecklists()
    {
        // Arrange
        var query = new GetAllChecklistsQuery();
        var checklists = new List<Checklist>
        {
            new()
            {
                Id = 1,
                LotId = 1,
                Date = DateTime.Today,
                UserId = 1,
            },
            new()
            {
                Id = 2,
                LotId = 1,
                Date = DateTime.Today.AddDays(-1),
                UserId = 1,
            },
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot { Id = 1, Name = "Test Lot" };

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetAllAsync())
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
    public async Task Handle_ReturnsEmptyList_WhenNoChecklistsExist()
    {
        // Arrange
        var query = new GetAllChecklistsQuery();
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Checklist>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
