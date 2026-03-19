using System.Linq.Expressions;
using AgroLink.Application.Features.Checklists.Queries.GetAll;
using AgroLink.Application.Interfaces;
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
    public async Task Handle_ReturnsChecklistsForCurrentFarm()
    {
        var farmId = 5;
        var paddockId = 10;
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
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            PaddockId = paddockId,
        };
        var paddock = new Paddock { Id = paddockId, FarmId = farmId };
        var user = new User { Id = 1, Name = "Test User" };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(checklists);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>()))
            .ReturnsAsync(new List<Lot> { lot });
        _mocker
            .GetMock<IPaddockRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Paddock, bool>>>()))
            .ReturnsAsync(new List<Paddock> { paddock });
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(new List<User> { user });
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(new List<ChecklistItem>());
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Animal, bool>>>()))
            .ReturnsAsync(new List<Animal>());

        var result = await _handler.Handle(new GetAllChecklistsQuery(), CancellationToken.None);

        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.First().LotName.ShouldBe(lot.Name);
        result.First().UserName.ShouldBe(user.Name);
    }

    [Test]
    public async Task Handle_NoFarmContext_ReturnsEmpty()
    {
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns((int?)null);

        var result = await _handler.Handle(new GetAllChecklistsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
        _mocker.GetMock<IChecklistRepository>().Verify(r => r.GetAllAsync(), Times.Never);
    }

    [Test]
    public async Task Handle_NoChecklistsExist_ReturnsEmpty()
    {
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(1);
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetAllAsync())
            .ReturnsAsync(new List<Checklist>());

        var result = await _handler.Handle(new GetAllChecklistsQuery(), CancellationToken.None);

        result.ShouldBeEmpty();
    }
}
