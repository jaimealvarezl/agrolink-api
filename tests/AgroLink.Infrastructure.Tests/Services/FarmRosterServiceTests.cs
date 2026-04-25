using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Services;

[TestFixture]
public class FarmRosterServiceTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        _context = CreateInMemoryContext();
        _cache = new MemoryCache(new MemoryCacheOptions());
        _service = new FarmRosterService(_context, _cache, NullLogger<FarmRosterService>.Instance);
    }

    [TearDown]
    public void TearDown()
    {
        _cache.Dispose();
        _context.Dispose();
    }

    private AgroLinkDbContext _context = null!;
    private MemoryCache _cache = null!;
    private FarmRosterService _service = null!;

    // ── basic queries ──────────────────────────────────────────────────────────

    [Test]
    public async Task GetRosterAsync_WithActiveAnimals_ReturnsOnlyActiveAnimals()
    {
        var (farm, paddock, lot) = await SeedFarmAsync();
        await SeedAnimalAsync(lot.Id, "Rosa");
        await SeedAnimalAsync(lot.Id, "Muerta", LifeStatus.Dead);

        var roster = await _service.GetRosterAsync(farm.Id);

        roster.Animals.Count.ShouldBe(1);
        roster.Animals[0].Name.ShouldBe("Rosa");
    }

    [Test]
    public async Task GetRosterAsync_WithActiveLots_ReturnsOnlyActiveLots()
    {
        var (farm, paddock, _) = await SeedFarmAsync();
        await SeedLotAsync(paddock.Id, "Lote Activo", "ACTIVE");
        await SeedLotAsync(paddock.Id, "Lote Inactivo", "INACTIVE");

        var roster = await _service.GetRosterAsync(farm.Id);

        roster.Lots.Count.ShouldBe(2); // the seeded lot + the inactive one
        roster.Lots.All(l => l.Name != "Lote Inactivo").ShouldBeTrue();
    }

    [Test]
    public async Task GetRosterAsync_AnimalsIncludeEarTagCuiaAndLotName()
    {
        var (farm, paddock, lot) = await SeedFarmAsync();
        var animal = await SeedAnimalAsync(lot.Id, "Rosa", LifeStatus.Active, "042", "NIC-042");

        var roster = await _service.GetRosterAsync(farm.Id);

        var entry = roster.Animals.Single();
        entry.Id.ShouldBe(animal.Id);
        entry.Name.ShouldBe("Rosa");
        entry.EarTag.ShouldBe("042");
        entry.Cuia.ShouldBe("NIC-042");
        entry.LotId.ShouldBe(lot.Id);
        entry.LotName.ShouldBe(lot.Name);
    }

    [Test]
    public async Task GetRosterAsync_LotsIncludePaddockIdAndName()
    {
        var (farm, paddock, lot) = await SeedFarmAsync();

        var roster = await _service.GetRosterAsync(farm.Id);

        var entry = roster.Lots.Single();
        entry.Id.ShouldBe(lot.Id);
        entry.Name.ShouldBe(lot.Name);
        entry.PaddockId.ShouldBe(paddock.Id);
        entry.PaddockName.ShouldBe(paddock.Name);
    }

    [Test]
    public async Task GetRosterAsync_FiltersAnimalsByFarmId()
    {
        var (farm1, paddock1, lot1) = await SeedFarmAsync("Farm A");
        var (farm2, paddock2, lot2) = await SeedFarmAsync("Farm B");
        await SeedAnimalAsync(lot1.Id, "Rosa");
        await SeedAnimalAsync(lot2.Id, "Lola");

        var roster = await _service.GetRosterAsync(farm1.Id);

        roster.Animals.Count.ShouldBe(1);
        roster.Animals[0].Name.ShouldBe("Rosa");
    }

    [Test]
    public async Task GetRosterAsync_EmptyFarm_ReturnsEmptyRoster()
    {
        var (farm, _, _) = await SeedFarmAsync();

        var roster = await _service.GetRosterAsync(farm.Id);

        roster.Animals.ShouldBeEmpty();
        roster.Lots.ShouldNotBeEmpty(); // the lot from SeedFarmAsync is active
    }

    // ── cache ──────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetRosterAsync_SecondCall_ReturnsCachedResult()
    {
        var (farm, _, lot) = await SeedFarmAsync();
        await SeedAnimalAsync(lot.Id, "Rosa");

        var first = await _service.GetRosterAsync(farm.Id);

        // Add a new animal after first call — cache should hide it
        await SeedAnimalAsync(lot.Id, "Nueva");

        var second = await _service.GetRosterAsync(farm.Id);

        second.ShouldBeSameAs(first);
        second.Animals.Count.ShouldBe(1);
    }

    [Test]
    public async Task GetRosterAsync_DifferentFarms_CachedSeparately()
    {
        var (farm1, _, lot1) = await SeedFarmAsync("Farm A");
        var (farm2, _, lot2) = await SeedFarmAsync("Farm B");
        await SeedAnimalAsync(lot1.Id, "Rosa");
        await SeedAnimalAsync(lot2.Id, "Lola");

        var roster1 = await _service.GetRosterAsync(farm1.Id);
        var roster2 = await _service.GetRosterAsync(farm2.Id);

        roster1.Animals[0].Name.ShouldBe("Rosa");
        roster2.Animals[0].Name.ShouldBe("Lola");
    }

    // ── animal cap ─────────────────────────────────────────────────────────────

    [Test]
    public async Task GetRosterAsync_WhenMoreThan500ActiveAnimals_CapsAt500()
    {
        var (farm, _, lot) = await SeedFarmAsync();
        for (var i = 0; i < 510; i++)
        {
            _context.Animals.Add(
                new Animal
                {
                    Name = $"Animal {i}",
                    LotId = lot.Id,
                    LifeStatus = LifeStatus.Active,
                    BirthDate = DateTime.UtcNow.AddYears(-1),
                    UpdatedAt = DateTime.UtcNow.AddSeconds(-i), // ensures descending order
                }
            );
        }

        await _context.SaveChangesAsync();

        var roster = await _service.GetRosterAsync(farm.Id);

        roster.Animals.Count.ShouldBe(500);
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private async Task<(Farm farm, Paddock paddock, Lot lot)> SeedFarmAsync(
        string name = "Test Farm"
    )
    {
        var owner = new Owner { Name = "Owner", Phone = "555" };
        _context.Owners.Add(owner);
        await _context.SaveChangesAsync();

        var farm = new Farm { Name = name, OwnerId = owner.Id };
        _context.Farms.Add(farm);
        await _context.SaveChangesAsync();

        var paddock = new Paddock { Name = $"{name} Paddock", FarmId = farm.Id };
        _context.Paddocks.Add(paddock);
        await _context.SaveChangesAsync();

        var lot = new Lot
        {
            Name = $"{name} Lot",
            PaddockId = paddock.Id,
            Status = "ACTIVE",
        };
        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();

        return (farm, paddock, lot);
    }

    private async Task<Animal> SeedAnimalAsync(
        int lotId,
        string name,
        LifeStatus lifeStatus = LifeStatus.Active,
        string? earTag = null,
        string? cuia = null
    )
    {
        var animal = new Animal
        {
            Name = name,
            TagVisual = earTag,
            Cuia = cuia,
            LotId = lotId,
            LifeStatus = lifeStatus,
            BirthDate = DateTime.UtcNow.AddYears(-1),
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Animals.Add(animal);
        await _context.SaveChangesAsync();
        return animal;
    }

    private async Task<Lot> SeedLotAsync(int paddockId, string name, string status)
    {
        var lot = new Lot
        {
            Name = name,
            PaddockId = paddockId,
            Status = status,
        };
        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();
        return lot;
    }
}
