using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.Queries.GetTimeline;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Animals.Queries.GetTimeline;

[TestFixture]
public class GetAnimalTimelineQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAnimalTimelineQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetAnimalTimelineQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ValidQuery_ReturnsMergedTimelineOrderedByDateDesc()
    {
        // Arrange
        const int animalId = 1;
        const int farmId = 10;
        var query = new GetAnimalTimelineQuery(animalId, farmId);
        var animal = new Animal { Id = animalId, Name = "Bessie" };

        var oldest = DateTime.UtcNow.AddDays(-10);
        var middle = DateTime.UtcNow.AddDays(-5);
        var newest = DateTime.UtcNow.AddDays(-1);

        var notes = new List<AnimalNote>
        {
            new()
            {
                Id = 1,
                AnimalId = animalId,
                Content = "Note at middle",
                UserId = 5,
                User = new User { Name = "Vet" },
                CreatedAt = middle,
            },
        };

        var movements = new List<Movement>
        {
            new()
            {
                Id = 1,
                AnimalId = animalId,
                UserId = 5,
                At = oldest,
                CreatedAt = oldest,
            },
        };

        var checklistItems = new List<ChecklistItem>
        {
            new()
            {
                Id = 1,
                ChecklistId = 100,
                AnimalId = animalId,
                Present = true,
                Condition = "OK",
                Checklist = new Checklist
                {
                    Id = 100,
                    LotId = 50,
                    Date = newest,
                    Lot = new Lot { Name = "Lot A" },
                },
            },
        };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(animalId, farmId))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(notes);
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetMovementsByAnimalAsync(animalId))
            .ReturnsAsync(movements);
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetItemsByAnimalIdAsync(animalId))
            .ReturnsAsync(checklistItems);
        _mocker
            .GetMock<IUserRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<User, bool>>>()))
            .ReturnsAsync(
                new List<User>
                {
                    new() { Id = 5, Name = "Vet" },
                }
            );
        _mocker
            .GetMock<ILotRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Lot, bool>>>()))
            .ReturnsAsync(new List<Lot>());

        // Act
        var result = (await _handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Count.ShouldBe(3);
        result[0].OccurredAt.ShouldBe(newest);
        result[0].Type.ShouldBe("checklist");
        result[1].OccurredAt.ShouldBe(middle);
        result[1].Type.ShouldBe("note");
        result[2].OccurredAt.ShouldBe(oldest);
        result[2].Type.ShouldBe("movement");
    }

    [Test]
    public async Task Handle_AnimalNotFoundInFarm_ThrowsNotFoundException()
    {
        // Arrange — covers both "doesn't exist" and "belongs to a different farm"
        var query = new GetAnimalTimelineQuery(999, 10);

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(999, 10))
            .ReturnsAsync((Animal?)null);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None)
        );
    }

    [Test]
    public async Task Handle_NoData_ReturnsEmptyList()
    {
        // Arrange
        const int animalId = 1;
        const int farmId = 10;
        var query = new GetAnimalTimelineQuery(animalId, farmId);
        var animal = new Animal { Id = animalId, Name = "Bessie" };

        _mocker
            .GetMock<IAnimalRepository>()
            .Setup(r => r.GetByIdInFarmAsync(animalId, farmId))
            .ReturnsAsync(animal);
        _mocker
            .GetMock<IAnimalNoteRepository>()
            .Setup(r => r.GetByAnimalIdAsync(animalId))
            .ReturnsAsync(new List<AnimalNote>());
        _mocker
            .GetMock<IMovementRepository>()
            .Setup(r => r.GetMovementsByAnimalAsync(animalId))
            .ReturnsAsync(new List<Movement>());
        _mocker
            .GetMock<IChecklistRepository>()
            .Setup(r => r.GetItemsByAnimalIdAsync(animalId))
            .ReturnsAsync(new List<ChecklistItem>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeEmpty();
    }
}
