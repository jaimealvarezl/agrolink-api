using System.Net;
using System.Text.Json;
using AgroLink.Application.Features.HerdComposition.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.HerdComposition;

public class HerdCompositionIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    // ── Helpers ──────────────────────────────────────────────────────────────

    private async Task<(Farm farm, Paddock paddock, User user)> SetupFarmAsync(
        string role = FarmMemberRoles.Editor
    )
    {
        var user = new User
        {
            Name = "Test User",
            Email = $"user-{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var farmOwner = new Owner { Name = "Farm Owner", Phone = "000" };
        DbContext.Owners.Add(farmOwner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = farmOwner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        var paddock = new Paddock { Name = "P1", FarmId = farm.Id };
        DbContext.Paddocks.Add(paddock);

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = role,
            }
        );
        await DbContext.SaveChangesAsync();

        return (farm, paddock, user);
    }

    private async Task<Lot> AddLotAsync(int paddockId, string name = "Lot A")
    {
        var lot = new Lot
        {
            Name = name,
            PaddockId = paddockId,
            Status = "ACTIVE",
        };
        DbContext.Lots.Add(lot);
        await DbContext.SaveChangesAsync();
        return lot;
    }

    private async Task<Animal> AddAnimalAsync(
        int lotId,
        Sex sex = Sex.Female,
        LifeStatus life = LifeStatus.Active
    )
    {
        var animal = new Animal
        {
            Name = $"Animal-{Guid.NewGuid():N}",
            Sex = sex,
            LotId = lotId,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = life,
            ProductionStatus = ProductionStatus.Milking,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        DbContext.Animals.Add(animal);
        await DbContext.SaveChangesAsync();
        return animal;
    }

    private async Task<Owner> AddOwnerAsync(string name)
    {
        var owner = new Owner { Name = name };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();
        return owner;
    }

    private async Task AssignOwnerAsync(int animalId, int ownerId)
    {
        DbContext.AnimalOwners.Add(
            new AnimalOwner
            {
                AnimalId = animalId,
                OwnerId = ownerId,
                SharePercent = 100,
            }
        );
        await DbContext.SaveChangesAsync();
    }

    private async Task<HerdCompositionDto> GetHerdCompositionAsync(Farm farm)
    {
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/herd-composition");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<HerdCompositionDto>(JsonOptions);
        result.ShouldNotBeNull();
        return result!;
    }

    // ── Auth ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task GetHerdComposition_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/farms/1/herd-composition");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetHerdComposition_NonMember_Returns403()
    {
        var (farm, _, _) = await SetupFarmAsync();

        var outsider = new User
        {
            Name = "Outsider",
            Email = $"out-{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(outsider);
        await DbContext.SaveChangesAsync();

        Authenticate(outsider);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/herd-composition");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetHerdComposition_ViewerRole_Returns403()
    {
        var (farm, _, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/herd-composition");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetHerdComposition_EditorRole_Returns200()
    {
        var (farm, _, user) = await SetupFarmAsync();

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/herd-composition");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    // ── Empty farm ────────────────────────────────────────────────────────────

    [Test]
    public async Task GetHerdComposition_EmptyFarm_ReturnsEmptyArrays()
    {
        var (farm, _, user) = await SetupFarmAsync();

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        result.ByOwnerGroup.ShouldBeEmpty();
        result.ByLot.ShouldBeEmpty();
        result.ByLotAndSex.ShouldBeEmpty();
    }

    // ── byLot ─────────────────────────────────────────────────────────────────

    [Test]
    public async Task ByLot_CountsActiveAnimalsPerLot()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lotA = await AddLotAsync(paddock.Id);
        var lotB = await AddLotAsync(paddock.Id, "Lot B");

        await AddAnimalAsync(lotA.Id);
        await AddAnimalAsync(lotA.Id);
        await AddAnimalAsync(lotB.Id);

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        result.ByLot.Count.ShouldBe(2);
        result.ByLot.Single(l => l.LotName == "Lot A").AnimalCount.ShouldBe(2);
        result.ByLot.Single(l => l.LotName == "Lot B").AnimalCount.ShouldBe(1);
    }

    [Test]
    public async Task ByLot_ExcludesRetiredAnimals()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        await AddAnimalAsync(lot.Id, life: LifeStatus.Active);
        await AddAnimalAsync(lot.Id, life: LifeStatus.Retired);

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        result.ByLot.Single().AnimalCount.ShouldBe(1);
    }

    [Test]
    public async Task ByLot_ExcludesLotsWithNoActiveAnimals()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var activeAnimalLot = await AddLotAsync(paddock.Id, "Has Animals");
        var emptyLot = await AddLotAsync(paddock.Id, "No Animals");

        await AddAnimalAsync(activeAnimalLot.Id);
        // emptyLot intentionally has no animals

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        result.ByLot.Count.ShouldBe(1);
        result.ByLot.ShouldNotContain(l => l.LotName == "No Animals");
    }

    // ── byLotAndSex ───────────────────────────────────────────────────────────

    [Test]
    public async Task ByLotAndSex_SplitsMaleAndFemaleCountsPerLot()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id, "Mixed");

        await AddAnimalAsync(lot.Id, Sex.Male);
        await AddAnimalAsync(lot.Id, Sex.Male);
        await AddAnimalAsync(lot.Id);

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        var entry = result.ByLotAndSex.Single(l => l.LotName == "Mixed");
        entry.MaleCount.ShouldBe(2);
        entry.FemaleCount.ShouldBe(1);
    }

    [Test]
    public async Task ByLotAndSex_MalePlusFemaleEqualsCorrespondingByLotCount()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        await AddAnimalAsync(lot.Id, Sex.Male);
        await AddAnimalAsync(lot.Id);
        await AddAnimalAsync(lot.Id);
        await AddAnimalAsync(lot.Id, life: LifeStatus.Retired); // excluded

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        var byLotCount = result.ByLot.Single().AnimalCount;
        var sexEntry = result.ByLotAndSex.Single();

        (sexEntry.MaleCount + sexEntry.FemaleCount).ShouldBe(byLotCount);
        byLotCount.ShouldBe(3);
    }

    // ── byOwnerGroup ──────────────────────────────────────────────────────────

    [Test]
    public async Task ByOwnerGroup_AnimalsWithNoOwners_AppearWithEmptyOwnerNames()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        await AddAnimalAsync(lot.Id); // no owners

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        result.ByOwnerGroup.Count.ShouldBe(1);
        result.ByOwnerGroup[0].OwnerNames.ShouldBeEmpty();
        result.ByOwnerGroup[0].AnimalCount.ShouldBe(1);
    }

    [Test]
    public async Task ByOwnerGroup_SameOwnerSet_CountedInSameGroup()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        var ownerA = await AddOwnerAsync("Alice");
        var ownerB = await AddOwnerAsync("Bob");

        var animal1 = await AddAnimalAsync(lot.Id);
        await AssignOwnerAsync(animal1.Id, ownerA.Id);
        await AssignOwnerAsync(animal1.Id, ownerB.Id);

        var animal2 = await AddAnimalAsync(lot.Id);
        await AssignOwnerAsync(animal2.Id, ownerA.Id);
        await AssignOwnerAsync(animal2.Id, ownerB.Id);

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        result.ByOwnerGroup.Count.ShouldBe(1);
        result.ByOwnerGroup[0].AnimalCount.ShouldBe(2);
        result.ByOwnerGroup[0].OwnerNames.ShouldBe(["Alice", "Bob"]);
    }

    [Test]
    public async Task ByOwnerGroup_DifferentOwnerSets_CountedInSeparateGroups()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        var ownerA = await AddOwnerAsync("Alice");
        var ownerB = await AddOwnerAsync("Bob");

        var animal1 = await AddAnimalAsync(lot.Id);
        await AssignOwnerAsync(animal1.Id, ownerA.Id);

        var animal2 = await AddAnimalAsync(lot.Id);
        await AssignOwnerAsync(animal2.Id, ownerB.Id);

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        result.ByOwnerGroup.Count.ShouldBe(2);
        var aliceGroup = result.ByOwnerGroup.Single(g => g.OwnerNames.Contains("Alice"));
        aliceGroup.AnimalCount.ShouldBe(1);
        var bobGroup = result.ByOwnerGroup.Single(g => g.OwnerNames.Contains("Bob"));
        bobGroup.AnimalCount.ShouldBe(1);
    }

    [Test]
    public async Task ByOwnerGroup_OwnerNamesAreSortedAlphabetically()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        var ownerZ = await AddOwnerAsync("Zara");
        var ownerA = await AddOwnerAsync("Ana");
        var ownerM = await AddOwnerAsync("Miguel");

        var animal = await AddAnimalAsync(lot.Id);
        // Assign in non-alphabetical order
        await AssignOwnerAsync(animal.Id, ownerZ.Id);
        await AssignOwnerAsync(animal.Id, ownerA.Id);
        await AssignOwnerAsync(animal.Id, ownerM.Id);

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        var group = result.ByOwnerGroup.Single();
        group.OwnerNames.ShouldBe(["Ana", "Miguel", "Zara"]);
    }

    [Test]
    public async Task ByOwnerGroup_TotalAnimalCountSumsToActiveHerd()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        var ownerA = await AddOwnerAsync("Alice");

        var animal1 = await AddAnimalAsync(lot.Id); // no owner
        var animal2 = await AddAnimalAsync(lot.Id);
        await AssignOwnerAsync(animal2.Id, ownerA.Id);
        await AddAnimalAsync(lot.Id, life: LifeStatus.Retired); // excluded

        Authenticate(user);
        var result = await GetHerdCompositionAsync(farm);

        var totalFromGroups = result.ByOwnerGroup.Sum(g => g.AnimalCount);
        totalFromGroups.ShouldBe(2); // 2 active animals
    }
}
