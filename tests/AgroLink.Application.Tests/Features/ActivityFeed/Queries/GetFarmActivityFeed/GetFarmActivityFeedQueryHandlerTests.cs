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

    [Test]
    public async Task Handle_WhenNoEvents_ReturnsEmptyList()
    {
        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

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

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync(movements);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 3), default)).ToList();

        result.Count.ShouldBe(3);
        result[0].OccurredAt.ShouldBe(base_.AddDays(9));
        result[1].OccurredAt.ShouldBe(base_.AddDays(8));
        result[2].OccurredAt.ShouldBe(base_.AddDays(7));
    }

    [Test]
    public async Task Handle_MovementWithToLot_DescriptionContainsLotName()
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

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([movement]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe("Movement");
        item.AnimalId.ShouldBe(42);
        item.AnimalName.ShouldBe("Manchita");
        item.Description.ShouldBe("Movido a Potrero Norte");
    }

    [Test]
    public async Task Handle_MovementWithNoToLot_UsesFallbackDescription()
    {
        var animal = MakeAnimal(1);
        var movement = new Movement
        {
            AnimalId = animal.Id,
            Animal = animal,
            ToLot = null,
            At = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([movement]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result[0].Description.ShouldBe("Movimiento registrado");
    }

    [Test]
    public async Task Handle_TimelineNote_DescriptionIsNoteContent()
    {
        var animal = MakeAnimal(7, "Lola");
        var note = new AnimalNote
        {
            AnimalId = animal.Id,
            Animal = animal,
            Content = "Se observó cojera leve",
            CreatedAt = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([note]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe("TimelineNote");
        item.AnimalId.ShouldBe(7);
        item.AnimalName.ShouldBe("Lola");
        item.Description.ShouldBe("Se observó cojera leve");
    }

    [Test]
    public async Task Handle_Retirement_DescriptionContainsReason()
    {
        var animal = MakeAnimal(3, "Bella");
        var retirement = new AnimalRetirement
        {
            AnimalId = animal.Id,
            Animal = animal,
            Reason = RetirementReason.Sold,
            At = DateTime.UtcNow,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([retirement]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe("Retirement");
        item.AnimalId.ShouldBe(3);
        item.Description.ShouldBe("Dado de baja: Sold");
    }

    [Test]
    public async Task Handle_NewbornRegistration_DescriptionAndOccurredAtAreMapped()
    {
        var birthDate = new DateTime(2026, 3, 10, 0, 0, 0, DateTimeKind.Utc);
        var animal = new Animal
        {
            Id = 99,
            Name = "Ternero",
            BirthDate = birthDate,
        };

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([animal]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(1);
        var item = result[0];
        item.EventType.ShouldBe("NewbornRegistration");
        item.AnimalId.ShouldBe(99);
        item.AnimalName.ShouldBe("Ternero");
        item.Description.ShouldBe("Nacimiento registrado");
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

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([note]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

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

        Repo.Setup(r => r.GetFarmMovementsAsync(1, default)).ReturnsAsync([movement]);
        Repo.Setup(r => r.GetFarmNotesAsync(1, default)).ReturnsAsync([note]);
        Repo.Setup(r => r.GetFarmRetirementsAsync(1, default)).ReturnsAsync([retirement]);
        Repo.Setup(r => r.GetFarmNewbornsAsync(1, default)).ReturnsAsync([]);

        var result = (await _handler.Handle(new GetFarmActivityFeedQuery(1, 5), default)).ToList();

        result.Count.ShouldBe(3);
        result[0].EventType.ShouldBe("Movement");
        result[1].EventType.ShouldBe("Retirement");
        result[2].EventType.ShouldBe("TimelineNote");
    }
}
