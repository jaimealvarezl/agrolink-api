using System.Linq.Expressions;
using AgroLink.Application.Features.Checklists.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Checklists.Queries.GetById;

[TestFixture]
public class GetChecklistByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _checklistRepositoryMock = new Mock<IChecklistRepository>();
        _checklistItemRepositoryMock = new Mock<IRepository<ChecklistItem>>();
        _userRepositoryMock = new Mock<IUserRepository>();
        _animalRepositoryMock = new Mock<IAnimalRepository>();
        _photoRepositoryMock = new Mock<IPhotoRepository>();
        _lotRepositoryMock = new Mock<ILotRepository>();
        _paddockRepositoryMock = new Mock<IPaddockRepository>();
        _handler = new GetChecklistByIdQueryHandler(
            _checklistRepositoryMock.Object,
            _checklistItemRepositoryMock.Object,
            _userRepositoryMock.Object,
            _animalRepositoryMock.Object,
            _photoRepositoryMock.Object,
            _lotRepositoryMock.Object,
            _paddockRepositoryMock.Object
        );
    }

    private Mock<IChecklistRepository> _checklistRepositoryMock = null!;
    private Mock<IRepository<ChecklistItem>> _checklistItemRepositoryMock = null!;
    private Mock<IUserRepository> _userRepositoryMock = null!;
    private Mock<IAnimalRepository> _animalRepositoryMock = null!;
    private Mock<IPhotoRepository> _photoRepositoryMock = null!;
    private Mock<ILotRepository> _lotRepositoryMock = null!;
    private Mock<IPaddockRepository> _paddockRepositoryMock = null!;
    private GetChecklistByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingChecklist_ReturnsChecklistDto()
    {
        // Arrange
        var checklistId = 1;
        var query = new GetChecklistByIdQuery(checklistId);
        var checklist = new Checklist
        {
            Id = checklistId,
            ScopeType = "LOT",
            ScopeId = 1,
            Date = DateTime.Today,
            UserId = 1,
            Notes = "Test Notes",
        };
        var user = new User { Id = 1, Name = "Test User" };
        var lot = new Lot { Id = 1, Name = "Test Lot" };
        var animal = new Animal
        {
            Id = 1,
            TagVisual = "A001",
            Cuia = "CUIA-A001",
            Name = "Animal 1",
        };
        var checklistItem = new ChecklistItem
        {
            ChecklistId = checklistId,
            AnimalId = animal.Id,
            Present = true,
            Condition = "OK",
        };

        _checklistRepositoryMock.Setup(r => r.GetByIdAsync(checklistId)).ReturnsAsync(checklist);
        _userRepositoryMock.Setup(r => r.GetByIdAsync(user.Id)).ReturnsAsync(user);
        _checklistItemRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<ChecklistItem, bool>>>()))
            .ReturnsAsync(new List<ChecklistItem> { checklistItem });
        _animalRepositoryMock.Setup(r => r.GetByIdAsync(animal.Id)).ReturnsAsync(animal);
        _photoRepositoryMock
            .Setup(r => r.GetPhotosByEntityAsync(It.IsAny<string>(), It.IsAny<int>()))
            .ReturnsAsync(new List<Photo>());
        _lotRepositoryMock.Setup(r => r.GetByIdAsync(lot.Id)).ReturnsAsync(lot);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(checklistId);
        result.ScopeName.ShouldBe(lot.Name);
        result.Items.Count.ShouldBe(1);
        result.Items.First().AnimalCuia.ShouldBe(animal.Cuia);
    }

    [Test]
    public async Task Handle_NonExistingChecklist_ReturnsNull()
    {
        // Arrange
        var checklistId = 999;
        var query = new GetChecklistByIdQuery(checklistId);

        _checklistRepositoryMock
            .Setup(r => r.GetByIdAsync(checklistId))
            .ReturnsAsync((Checklist?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
