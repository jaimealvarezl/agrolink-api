using AgroLink.Application.Features.HerdComposition.Queries.GetHerdComposition;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.HerdComposition.Queries.GetHerdComposition;

[TestFixture]
public class GetHerdCompositionQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetHerdCompositionQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetHerdCompositionQueryHandler _handler = null!;

    private Mock<IHerdCompositionRepository> Repo => _mocker.GetMock<IHerdCompositionRepository>();

    private void SetupEmpty(int farmId = 1)
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(farmId, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(farmId, default)).ReturnsAsync([]);
    }

    // ── Empty ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task Handle_NoAnimals_ReturnsAllEmptyArrays()
    {
        SetupEmpty();

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByLot.ShouldBeEmpty();
        result.ByLotAndSex.ShouldBeEmpty();
        result.ByOwnerGroup.ShouldBeEmpty();
    }

    // ── byLot ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ByLot_SumsCountsAcrossSexesPerLot()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default))
            .ReturnsAsync([
                new LotSexRow(10, "Paridas", Sex.Female, 8),
                new LotSexRow(10, "Paridas", Sex.Male, 2),
            ]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByLot.Count.ShouldBe(1);
        result.ByLot[0].LotId.ShouldBe(10);
        result.ByLot[0].LotName.ShouldBe("Paridas");
        result.ByLot[0].AnimalCount.ShouldBe(10);
    }

    [Test]
    public async Task ByLot_MultipleLots_EachHasCorrectCount()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default))
            .ReturnsAsync([
                new LotSexRow(1, "Lot A", Sex.Female, 5),
                new LotSexRow(2, "Lot B", Sex.Male, 3),
                new LotSexRow(2, "Lot B", Sex.Female, 1),
            ]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByLot.Count.ShouldBe(2);
        result.ByLot.Single(l => l.LotId == 1).AnimalCount.ShouldBe(5);
        result.ByLot.Single(l => l.LotId == 2).AnimalCount.ShouldBe(4);
    }

    [Test]
    public async Task ByLot_IsOrderedByLotName()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default))
            .ReturnsAsync([
                new LotSexRow(2, "Zebra Lot", Sex.Female, 1),
                new LotSexRow(1, "Alpha Lot", Sex.Female, 1),
            ]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByLot[0].LotName.ShouldBe("Alpha Lot");
        result.ByLot[1].LotName.ShouldBe("Zebra Lot");
    }

    // ── byLotAndSex ───────────────────────────────────────────────────────────

    [Test]
    public async Task ByLotAndSex_SplitsMaleAndFemaleCounts()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default))
            .ReturnsAsync([
                new LotSexRow(10, "Paridas", Sex.Female, 14),
                new LotSexRow(10, "Paridas", Sex.Male, 8),
            ]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        var entry = result.ByLotAndSex.Single();
        entry.LotId.ShouldBe(10);
        entry.MaleCount.ShouldBe(8);
        entry.FemaleCount.ShouldBe(14);
    }

    [Test]
    public async Task ByLotAndSex_MalePlusFemaleEqualsCorrespondingByLotCount()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default))
            .ReturnsAsync([
                new LotSexRow(1, "Lot A", Sex.Female, 6),
                new LotSexRow(1, "Lot A", Sex.Male, 4),
                new LotSexRow(2, "Lot B", Sex.Female, 3),
            ]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        foreach (var lotSex in result.ByLotAndSex)
        {
            var byLotEntry = result.ByLot.Single(l => l.LotId == lotSex.LotId);
            (lotSex.MaleCount + lotSex.FemaleCount).ShouldBe(byLotEntry.AnimalCount);
        }
    }

    [Test]
    public async Task ByLotAndSex_AllFemaleLot_HasZeroMaleCount()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default))
            .ReturnsAsync([new LotSexRow(10, "Vaquillonas", Sex.Female, 5)]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default)).ReturnsAsync([]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByLotAndSex.Single().MaleCount.ShouldBe(0);
        result.ByLotAndSex.Single().FemaleCount.ShouldBe(5);
    }

    // ── byOwnerGroup ──────────────────────────────────────────────────────────

    [Test]
    public async Task ByOwnerGroup_AnimalsWithNoOwners_GroupedWithEmptyOwnerNames()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default))
            .ReturnsAsync([new AnimalOwnerRow(1, []), new AnimalOwnerRow(2, [])]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByOwnerGroup.Count.ShouldBe(1);
        result.ByOwnerGroup[0].OwnerNames.ShouldBeEmpty();
        result.ByOwnerGroup[0].AnimalCount.ShouldBe(2);
    }

    [Test]
    public async Task ByOwnerGroup_SameOwnerSet_MergedIntoOneGroup()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default))
            .ReturnsAsync([
                new AnimalOwnerRow(1, ["Alice", "Bob"]),
                new AnimalOwnerRow(2, ["Bob", "Alice"]), // same set, different order
            ]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByOwnerGroup.Count.ShouldBe(1);
        result.ByOwnerGroup[0].AnimalCount.ShouldBe(2);
    }

    [Test]
    public async Task ByOwnerGroup_DifferentOwnerSets_ProduceSeparateGroups()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default))
            .ReturnsAsync([
                new AnimalOwnerRow(1, ["Alice"]),
                new AnimalOwnerRow(2, ["Bob"]),
                new AnimalOwnerRow(3, ["Alice", "Bob"]),
            ]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByOwnerGroup.Count.ShouldBe(3);
    }

    [Test]
    public async Task ByOwnerGroup_OwnerNamesWithinGroupAreSortedAlphabetically()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default))
            .ReturnsAsync([new AnimalOwnerRow(1, ["Zara", "Ana", "Miguel"])]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByOwnerGroup.Single().OwnerNames.ShouldBe(["Ana", "Miguel", "Zara"]);
    }

    [Test]
    public async Task ByOwnerGroup_TotalCountMatchesTotalAnimalRows()
    {
        Repo.Setup(r => r.GetLotSexGroupsAsync(1, default)).ReturnsAsync([]);
        Repo.Setup(r => r.GetAnimalOwnerRowsAsync(1, default))
            .ReturnsAsync([
                new AnimalOwnerRow(1, []),
                new AnimalOwnerRow(2, ["Alice"]),
                new AnimalOwnerRow(3, ["Alice"]),
                new AnimalOwnerRow(4, ["Bob"]),
            ]);

        var result = await _handler.Handle(new GetHerdCompositionQuery(1), default);

        result.ByOwnerGroup.Sum(g => g.AnimalCount).ShouldBe(4);
    }
}
