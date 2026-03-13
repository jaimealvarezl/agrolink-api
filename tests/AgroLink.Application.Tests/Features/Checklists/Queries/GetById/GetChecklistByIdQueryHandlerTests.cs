using System.Linq.Expressions;
using AgroLink.Application.Features.Checklists.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Queries.GetById;

[TestFixture]
public class GetChecklistByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetChecklistByIdQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetChecklistByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingChecklist_ReturnsChecklistDto()
    {
        // Arrange
        var checklistId = 1;
        var farmId = 10;
        var query = new GetChecklistByIdQuery(checklistId);
        var checklist = new Checklist
        {
            Id = checklistId,
            LotId = 1,
            Date = DateTime.Today,
            UserId = 1,
            Notes = "Test Notes",
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot
        {
            Id = 1,
            Name = "Test Lot",
            Paddock = new Paddock { FarmId = farmId },
        };
        var animal = new Animal
        {
            Id = 1,
            TagVisual = "V001",
            Cuia = "CUIA-A001",
            Name = "Test Animal",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = 1,
        };
        var checklistItem = new ChecklistItem
        {
            ChecklistId = checklistId,
            AnimalId = animal.Id,
            Present = true,
            Condition = "OK",
        };

        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(farmId);
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(checklist);
        _mocker.GetMock<IUserRepository>().Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _mocker
            .GetMock<IRepository<ChecklistItem>>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(new List<ChecklistItem> { checklistItem });
        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Animal, bool>>>()))
            .ReturnsAsync(new List<Animal> { animal });
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>()))
            .ReturnsAsync(new List<Lot> { lot });
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.GetLotWithPaddockAsync(lot.Id))
            .ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(checklistId);
        result.LotName.ShouldBe(lot.Name);
        result.Items.Count.ShouldBe(1);
        result.Items.First().AnimalCuia.ShouldBe(animal.Cuia);
    }

    [Test]
    public async Task Handle_ChecklistFromAnotherFarm_ReturnsNull()
    {
        // Arrange
        var checklistId = 1;
        var currentFarmId = 10;
        var checklistFarmId = 20;
        var query = new GetChecklistByIdQuery(checklistId);
        var checklist = new Checklist { Id = checklistId, LotId = 1 };
        var lot = new Lot
        {
            Id = 1,
            Paddock = new Paddock { FarmId = checklistFarmId },
        };

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync(checklist);
        _mocker.GetMock<ILotRepository>().Setup(r => r.GetLotWithPaddockAsync(1)).ReturnsAsync(lot);
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmId).Returns(currentFarmId);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NonExistingChecklist_ReturnsNull()
    {
        // Arrange
        var checklistId = 999;
        var query = new GetChecklistByIdQuery(checklistId);

        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync((Checklist?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
