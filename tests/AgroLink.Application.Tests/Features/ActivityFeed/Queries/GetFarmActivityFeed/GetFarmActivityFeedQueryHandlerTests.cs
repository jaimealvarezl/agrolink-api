using AgroLink.Application.Features.ActivityFeed.DTOs;
using AgroLink.Application.Features.ActivityFeed.Queries.GetFarmActivityFeed;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.ActivityFeed.Queries.GetFarmActivityFeed;

[TestFixture]
public class GetFarmActivityFeedQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetFarmActivityFeedQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetFarmActivityFeedQueryHandler _handler = null!;

    private Mock<IFarmActivityFeedRepository> Repo =>
        _mocker.GetMock<IFarmActivityFeedRepository>();

    private static Animal MakeAnimal(int id, string name = "Manchita")
    {
        return new Animal { Id = id, Name = name };
    }

    private static Lot MakeLot(string name)
    {
        return new Lot { Name = name };
    }

    private void SetupEmpty(int farmId = 1, int limit = 5)
    {
        Repo.Setup(r => r.GetFarmMovementsAsync(farmId, limit, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(farmId, limit, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(farmId, limit, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(farmId, limit, default)).ReturnsAsync([]);
    }

    [Test]
    public async Task Handle_WhenNoEvents_ReturnsEmptyList()
    {
        SetupEmpty();

        var result = await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default);

        result.ShouldBeEmpty();
    }

    [Test]
    public async Task Handle_WhenLimitApplied_ReturnsOnlyTopNByOccurredAt()
    {
        var base_ = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var animal = MakeAnimal(1);
        var movements = Enumerable
            .Range(0, 10)
            .Select(i => new Movement
            {
                AnimalId = animal.Id,
                Animal = animal,
                At = base_.AddDays(i),
            })
            .ToList();

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 3, default)).ReturnsAsync(movements);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 3, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 3, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 3, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 3), default)).ToList();

        result.Count.ShouldBe(3);
        result[0].OccurredAt.ShouldBe(base_.AddDays(9));
        result[1].OccurredAt.ShouldBe(base_.AddDays(8));
        result[2].OccurredAt.ShouldBe(base_.AddDays(7));
    }

    [Test]
    public async Task Handle_MovementWithToLot_ExposesToLotName()
    {
        var animal = MakeAnimal(42);
        var lot = MakeLot("Potrero Norte");
        var movement = new Movement
        {
            AnimalId = animal.Id,
            Animal = animal,
            ToLot = lot,
            At = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 5, default)).ReturnsAsync([movement]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 5, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe(ActivityFeedEventType.Movement);
        item.AnimalId.ShouldBe(42);
        item.AnimalName.ShouldBe("Manchita");
        item.ToLotName.ShouldBe("Potrero Norte");
    }

    [Test]
    public async Task Handle_MovementWithNoToLot_ToLotNameIsNull()
    {
        var animal = MakeAnimal(1);
        var movement = new Movement
        {
            AnimalId = animal.Id,
            Animal = animal,
            ToLot = null,
            At = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 5, default)).ReturnsAsync([movement]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 5, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result[0].ToLotName.ShouldBeNull();
    }

    [Test]
    public async Task Handle_TimelineNote_ExposesNoteContent()
    {
        var animal = MakeAnimal(7, "Lola");
        var note = new AnimalNote
        {
            AnimalId = animal.Id,
            Animal = animal,
            Content = "Se observó cojera leve",
            CreatedAt = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 5, default)).ReturnsAsync([note]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 5, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe(ActivityFeedEventType.TimelineNote);
        item.AnimalId.ShouldBe(7);
        item.AnimalName.ShouldBe("Lola");
        item.NoteContent.ShouldBe("Se observó cojera leve");
    }

    [Test]
    public async Task Handle_Retirement_ExposesRetirementReason()
    {
        var animal = MakeAnimal(3, "Bella");
        var retirement = new AnimalRetirement
        {
            AnimalId = animal.Id,
            Animal = animal,
            Reason = RetirementReason.Sold,
            At = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 5, default)).ReturnsAsync([retirement]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 5, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe(ActivityFeedEventType.Retirement);
        item.AnimalId.ShouldBe(3);
        item.RetirementReason.ShouldBe("Sold");
    }

    [Test]
    public async Task Handle_NewbornRegistration_OccurredAtIsBirthDate()
    {
        var birthDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var animal = new Animal
        {
            Id = 99,
            Name = "Ternero",
            BirthDate = birthDate,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 5, default)).ReturnsAsync([animal]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe(ActivityFeedEventType.NewbornRegistration);
        item.AnimalId.ShouldBe(99);
        item.AnimalName.ShouldBe("Ternero");
        item.OccurredAt.ShouldBe(birthDate);
    }

    [Test]
    public async Task Handle_AnimalWithEmptyName_AnimalNameIsNull()
    {
        var animal = MakeAnimal(5, string.Empty);
        var note = new AnimalNote
        {
            AnimalId = animal.Id,
            Animal = animal,
            Content = "Nota",
            CreatedAt = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 5, default)).ReturnsAsync([note]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 5, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 5, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result[0].AnimalName.ShouldBeNull();
    }

    [Test]
    public async Task Handle_MixedEventTypes_SortedByOccurredAtDescending()
    {
        var now = DateTime.UtcNow;
        var animal = MakeAnimal(1, "A");

        var movement = new Movement
        {
            AnimalId = animal.Id,
            Animal = animal,
            At = now.AddHours(-1),
        };
        var note = new AnimalNote
        {
            AnimalId = animal.Id,
            Animal = animal,
            Content = "x",
            CreatedAt = now.AddHours(-3),
        };
        var retirement = new AnimalRetirement
        {
            AnimalId = animal.Id,
            Animal = animal,
            Reason = RetirementReason.Dead,
            At = now.AddHours(-2),
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, 5, default)).ReturnsAsync([movement]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, 5, default)).ReturnsAsync([note]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, 5, default)).ReturnsAsync([retirement]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, 5, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(3);
        result[0].EventType.ShouldBe(ActivityFeedEventType.Movement);
        result[1].EventType.ShouldBe(ActivityFeedEventType.Retirement);
        result[2].EventType.ShouldBe(ActivityFeedEventType.TimelineNote);
    }
}
