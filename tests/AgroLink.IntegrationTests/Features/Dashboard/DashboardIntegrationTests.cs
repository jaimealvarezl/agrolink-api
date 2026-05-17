using System.Net;
using System.Text.Json;
using AgroLink.Application.Features.Dashboard.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Dashboard;

public class DashboardIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private async Task<(Farm farm, Paddock paddock, User user)> SetupFarmAsync(
        string role = FarmMemberRoles.Viewer
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

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
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
        LifeStatus life = LifeStatus.Active,
        HealthStatus health = HealthStatus.Healthy
    )
    {
        var animal = new Animal
        {
            Name = $"Animal-{Guid.NewGuid():N}",
            Sex = Sex.Female,
            LotId = lotId,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = life,
            ProductionStatus = ProductionStatus.Milking,
            HealthStatus = health,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        DbContext.Animals.Add(animal);
        await DbContext.SaveChangesAsync();
        return animal;
    }

    private async Task<Checklist> AddChecklistAsync(
        int lotId,
        int userId,
        DateTime? createdAt = null
    )
    {
        var checklist = new Checklist
        {
            LotId = lotId,
            UserId = userId,
            Date = (createdAt ?? DateTime.UtcNow).Date,
            CreatedAt = createdAt ?? DateTime.UtcNow,
        };
        DbContext.Checklists.Add(checklist);
        await DbContext.SaveChangesAsync();
        return checklist;
    }

    private async Task AddChecklistItemAsync(
        int checklistId,
        int animalId,
        bool present,
        string condition
    )
    {
        DbContext.ChecklistItems.Add(
            new ChecklistItem
            {
                ChecklistId = checklistId,
                AnimalId = animalId,
                Present = present,
                Condition = condition,
            }
        );
        await DbContext.SaveChangesAsync();
    }

    // --- Counts ---

    [Test]
    public async Task GetSummary_ReturnsCorrectHerdCount()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        await AddAnimalAsync(lot.Id);
        await AddAnimalAsync(lot.Id);
        await AddAnimalAsync(lot.Id, LifeStatus.Retired); // should not count

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result.ShouldNotBeNull();
        result!.HerdCount.ShouldBe(2);
    }

    [Test]
    public async Task GetSummary_ReturnsCorrectSickCount()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        await AddAnimalAsync(lot.Id, health: HealthStatus.Healthy);
        await AddAnimalAsync(lot.Id, health: HealthStatus.Sick);
        await AddAnimalAsync(lot.Id, health: HealthStatus.Sick);

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.SickCount.ShouldBe(2);
    }

    // --- Checklist aggregation ---

    [Test]
    public async Task GetSummary_NoChecklists_ReturnsNullLastChecklistDateAndZeroCounts()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        await AddLotAsync(paddock.Id);

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.LastChecklistDate.ShouldBeNull();
        result.LastChecklistIssueCount.ShouldBe(0);
        result.NovedadCount.ShouldBe(0);
    }

    [Test]
    public async Task GetSummary_ReturnsNovedadCountFromMostRecentSessionOnly()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);
        var animal = await AddAnimalAsync(lot.Id);
        var animal2 = await AddAnimalAsync(lot.Id); // unique index requires distinct animals per checklist

        // Older session: 2 issues — should be ignored
        var older = await AddChecklistAsync(lot.Id, user.Id, DateTime.UtcNow.AddDays(-3));
        await AddChecklistItemAsync(older.Id, animal.Id, true, "OBS");
        await AddChecklistItemAsync(older.Id, animal2.Id, true, "URG");

        // Most recent session: 1 novedad (present + issue)
        var latest = await AddChecklistAsync(lot.Id, user.Id, DateTime.UtcNow.AddDays(-1));
        await AddChecklistItemAsync(latest.Id, animal.Id, true, "OBS");
        await AddChecklistItemAsync(latest.Id, animal2.Id, false, "URG"); // not present — excluded from novedadCount

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.NovedadCount.ShouldBe(1);
        result.LastChecklistIssueCount.ShouldBe(2); // both OBS + URG count as issues
    }

    [Test]
    public async Task GetSummary_LastChecklistDate_IsFromMostRecentSession()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id);

        var expected = DateTime.UtcNow.AddDays(-1);
        await AddChecklistAsync(lot.Id, user.Id, DateTime.UtcNow.AddDays(-5));
        await AddChecklistAsync(lot.Id, user.Id, expected);

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.LastChecklistDate.ShouldNotBeNull();
        result.LastChecklistDate!.Value.ShouldBe(expected, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task GetSummary_AggregatesNovedadCountAcrossAllLots()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lotA = await AddLotAsync(paddock.Id);
        var lotB = await AddLotAsync(paddock.Id, "Lot B");
        var animal = await AddAnimalAsync(lotA.Id);
        var animalB = await AddAnimalAsync(lotB.Id);
        var animalB2 = await AddAnimalAsync(lotB.Id); // unique index requires distinct animals per checklist

        // Lot A latest session: 1 novedad
        var sessionA = await AddChecklistAsync(lotA.Id, user.Id, DateTime.UtcNow.AddHours(-2));
        await AddChecklistItemAsync(sessionA.Id, animal.Id, true, "OBS");

        // Lot B latest session: 2 novedades
        var sessionB = await AddChecklistAsync(lotB.Id, user.Id, DateTime.UtcNow.AddHours(-1));
        await AddChecklistItemAsync(sessionB.Id, animalB.Id, true, "OBS");
        await AddChecklistItemAsync(sessionB.Id, animalB2.Id, true, "URG");

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.NovedadCount.ShouldBe(3);
        result.LastChecklistIssueCount.ShouldBe(3);
    }

    // --- Overdue lots ---

    [Test]
    public async Task GetSummary_LotWithNoChecklist_IsOverdue()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id, "No Checklist Lot");

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.OverdueLots.ShouldContain(l =>
            l.LotId == lot.Id && l.LotName == "No Checklist Lot"
        );
    }

    [Test]
    public async Task GetSummary_LotWithRecentChecklist_IsNotOverdue()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id, "Recent Lot");

        await AddChecklistAsync(lot.Id, user.Id, DateTime.UtcNow.AddDays(-2));

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.OverdueLots.ShouldNotContain(l => l.LotId == lot.Id);
    }

    [Test]
    public async Task GetSummary_LotWithChecklistOlderThan7Days_IsOverdue()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var lot = await AddLotAsync(paddock.Id, "Stale Lot");

        await AddChecklistAsync(lot.Id, user.Id, DateTime.UtcNow.AddDays(-10));

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.OverdueLots.ShouldContain(l => l.LotId == lot.Id);
        result
            .OverdueLots.First(l => l.LotId == lot.Id)
            .DaysSinceLastChecklist.ShouldBeGreaterThanOrEqualTo(10);
    }

    [Test]
    public async Task GetSummary_MixedLots_ReturnsOnlyOverdueLots()
    {
        var (farm, paddock, user) = await SetupFarmAsync();
        var recentLot = await AddLotAsync(paddock.Id, "Recent");
        var overdueLot = await AddLotAsync(paddock.Id, "Overdue");

        await AddChecklistAsync(recentLot.Id, user.Id, DateTime.UtcNow.AddDays(-1));
        await AddChecklistAsync(overdueLot.Id, user.Id, DateTime.UtcNow.AddDays(-9));

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.OverdueLots.Count.ShouldBe(1);
        result.OverdueLots[0].LotId.ShouldBe(overdueLot.Id);
    }

    // --- milkToday ---

    [Test]
    public async Task GetSummary_MilkTodayIsAlwaysNull()
    {
        var (farm, _, user) = await SetupFarmAsync();

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result!.MilkToday.ShouldBeNull();
    }

    // --- Auth ---

    [Test]
    public async Task GetSummary_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/farms/1/dashboard-summary");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetSummary_NonMember_Returns403()
    {
        var (farm, _, _) = await SetupFarmAsync();

        var outsider = new User
        {
            Name = "Outsider",
            Email = $"outsider-{Guid.NewGuid()}@test.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(outsider);
        await DbContext.SaveChangesAsync();

        Authenticate(outsider);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetSummary_EmptyFarm_ReturnsZeroCountsAndEmptyOverdueLots()
    {
        var (farm, _, user) = await SetupFarmAsync();

        Authenticate(user);
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/dashboard-summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<DashboardSummaryDto>(JsonOptions);
        result.ShouldNotBeNull();
        result!.HerdCount.ShouldBe(0);
        result.SickCount.ShouldBe(0);
        result.NovedadCount.ShouldBe(0);
        result.OverdueLots.ShouldBeEmpty();
        result.LastChecklistDate.ShouldBeNull();
        result.MilkToday.ShouldBeNull();
    }
}
