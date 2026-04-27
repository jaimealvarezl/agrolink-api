using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Services;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Services;

[TestFixture]
public class EntityResolutionServiceTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        _context = CreateInMemoryContext();
        _service = new EntityResolutionService(_context);
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private AgroLinkDbContext _context = null!;
    private EntityResolutionService _service = null!;

    // ── Normalize helper ───────────────────────────────────────────────────────

    [Test]
    [TestCase("La Bonita", "bonita")]
    [TestCase("el rey del monte", "rey del monte")]
    [TestCase("ROSA", "rosa")]
    [TestCase("Ángel", "angel")]
    [TestCase("la llorona", "llorona")]
    [TestCase("El Toro Grande", "toro grande")]
    [TestCase("los becerros", "becerros")]
    public void Normalize_AppliesLowercaseAccentStrippingAndArticleRemoval(
        string input,
        string expected
    )
    {
        EntityResolutionService.Normalize(input).ShouldBe(expected);
    }

    // ── animal resolution ──────────────────────────────────────────────────────

    [Test]
    public async Task ResolveAsync_AnimalByName_ReturnsId()
    {
        var (farm, _, _, animal) = await SeedAsync("la bonita");
        animal.SearchText = EntityResolutionService.Normalize(animal.Name);
        await _context.SaveChangesAsync();

        var result = await _service.ResolveAsync(farm.Id, "la bonita", null, null, null);

        result.Animal?.Id.ShouldBe(animal.Id);
    }

    [Test]
    public async Task ResolveAsync_AnimalByEarTag_ReturnsId()
    {
        var (farm, _, _, animal) = await SeedAsync("la tica", "2299");
        animal.SearchText = "2299"; // SearchText = normalized ear tag → tier-1 exact match
        await _context.SaveChangesAsync();

        var result = await _service.ResolveAsync(farm.Id, "2299", null, null, null);

        result.Animal?.Id.ShouldBe(animal.Id);
    }

    [Test]
    public async Task ResolveAsync_AnimalByNormalizedName_StripsArticle()
    {
        var (farm, _, _, animal) = await SeedAsync("bonita");
        animal.SearchText = EntityResolutionService.Normalize(animal.Name);
        await _context.SaveChangesAsync();

        var result = await _service.ResolveAsync(farm.Id, "La Bonita", null, null, null);

        result.Animal?.Id.ShouldBe(animal.Id);
    }

    [Test]
    public async Task ResolveAsync_AnimalNotFound_ReturnsNull()
    {
        var (farm, _, _, _) = await SeedAsync("rosa");

        var result = await _service.ResolveAsync(farm.Id, "xyz999", null, null, null);

        result.Animal.ShouldBeNull();
    }

    [Test]
    public async Task ResolveAsync_AnimalFromWrongFarm_ReturnsNull()
    {
        var (farm, _, _, animal) = await SeedAsync("rosa");
        animal.SearchText = EntityResolutionService.Normalize(animal.Name);
        await _context.SaveChangesAsync();

        var result = await _service.ResolveAsync(farm.Id + 99, "rosa", null, null, null);

        result.Animal.ShouldBeNull();
    }

    [Test]
    public async Task ResolveAsync_ArticleOnlyMention_ReturnsNull()
    {
        var (farm, _, _, _) = await SeedAsync("rosa");

        var result = await _service.ResolveAsync(farm.Id, "la", null, null, null);

        result.Animal.ShouldBeNull();
    }

    // ── lot resolution ─────────────────────────────────────────────────────────

    [Test]
    public async Task ResolveAsync_LotByName_ReturnsId()
    {
        var (farm, _, lot, _) = await SeedAsync("rosa");
        lot.SearchText = EntityResolutionService.Normalize(lot.Name);
        await _context.SaveChangesAsync();

        var result = await _service.ResolveAsync(farm.Id, null, lot.Name, null, null);

        result.Lot?.Id.ShouldBe(lot.Id);
    }

    [Test]
    public async Task ResolveAsync_LotArticleStripped_StillMatches()
    {
        var (farm, _, lot, _) = await SeedAsync("rosa", lotName: "forro");
        lot.SearchText = EntityResolutionService.Normalize(lot.Name);
        await _context.SaveChangesAsync();

        var result = await _service.ResolveAsync(farm.Id, null, "el forro", null, null);

        result.Lot?.Id.ShouldBe(lot.Id);
    }

    // ── mother resolution ──────────────────────────────────────────────────────

    [Test]
    public async Task ResolveAsync_MotherMention_ResolvesViaAnimalTable()
    {
        var (farm, _, _, animal) = await SeedAsync("la milagro");
        animal.SearchText = EntityResolutionService.Normalize(animal.Name);
        await _context.SaveChangesAsync();

        var result = await _service.ResolveAsync(farm.Id, null, null, null, "la milagro");

        result.Mother?.Id.ShouldBe(animal.Id);
    }

    // ── all nulls ──────────────────────────────────────────────────────────────

    [Test]
    public async Task ResolveAsync_AllNullMentions_ReturnsAllNulls()
    {
        var result = await _service.ResolveAsync(1, null, null, null, null);

        result.Animal.ShouldBeNull();
        result.Lot.ShouldBeNull();
        result.TargetPaddock.ShouldBeNull();
        result.Mother.ShouldBeNull();
    }

    // ── helpers ────────────────────────────────────────────────────────────────

    private async Task<(Farm farm, Paddock paddock, Lot lot, Animal animal)> SeedAsync(
        string animalName,
        string? earTag = null,
        string lotName = "Lote Test"
    )
    {
        var owner = new Owner { Name = "Owner", Phone = "555" };
        _context.Owners.Add(owner);
        await _context.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
        _context.Farms.Add(farm);
        await _context.SaveChangesAsync();

        var paddock = new Paddock { Name = "Potrero Test", FarmId = farm.Id };
        _context.Paddocks.Add(paddock);
        await _context.SaveChangesAsync();

        var lot = new Lot
        {
            Name = lotName,
            PaddockId = paddock.Id,
            Status = "ACTIVE",
        };
        _context.Lots.Add(lot);
        await _context.SaveChangesAsync();

        var animal = new Animal
        {
            Name = animalName,
            TagVisual = earTag,
            LotId = lot.Id,
            LifeStatus = LifeStatus.Active,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            UpdatedAt = DateTime.UtcNow,
        };
        _context.Animals.Add(animal);
        await _context.SaveChangesAsync();

        return (farm, paddock, lot, animal);
    }
}
