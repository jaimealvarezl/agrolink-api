using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.MilkLogs.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.MilkLogs;

public class MilkLogsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private static readonly DateOnly Today = DateOnly.FromDateTime(DateTime.UtcNow);

    private async Task<(Farm farm, User user)> SetupFarmAsync(string role = FarmMemberRoles.Editor)
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

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = role,
            }
        );
        await DbContext.SaveChangesAsync();

        return (farm, user);
    }

    private async Task<DailyMilkLog> SeedLogAsync(
        int farmId,
        int userId,
        DateOnly date,
        decimal liters = 500m,
        decimal? pricePerLiter = null,
        string? notes = null
    )
    {
        var log = new DailyMilkLog
        {
            FarmId = farmId,
            Date = date,
            TotalLiters = liters,
            PricePerLiter = pricePerLiter,
            UserId = userId,
            Notes = notes,
            CreatedAt = DateTime.UtcNow,
        };
        DbContext.DailyMilkLogs.Add(log);
        await DbContext.SaveChangesAsync();
        return log;
    }

    private static StringContent JsonBody(object obj)
    {
        return new StringContent(JsonSerializer.Serialize(obj), Encoding.UTF8, "application/json");
    }

    private static string D(DateOnly date)
    {
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    // --- POST: create ---

    [Test]
    public async Task Post_NewLog_Returns201WithLog()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 300.5 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<MilkLogDto>(JsonOptions);
        dto.ShouldNotBeNull();
        dto!.FarmId.ShouldBe(farm.Id);
        dto.TotalLiters.ShouldBe(300.5m);
        dto.Date.ShouldBe(Today);
    }

    [Test]
    public async Task Post_ExistingLogSameDate_Returns200WithUpdatedLog()
    {
        var (farm, user) = await SetupFarmAsync();
        await SeedLogAsync(farm.Id, user.Id, Today, 200m);
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 999.99 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<MilkLogDto>(JsonOptions);
        dto!.TotalLiters.ShouldBe(999.99m);
    }

    [Test]
    public async Task Post_ZeroLiters_Returns201()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 0 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<MilkLogDto>(JsonOptions);
        dto!.TotalLiters.ShouldBe(0m);
    }

    [Test]
    public async Task Post_WithPricePerLiter_Returns201AndComputesRevenue()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(
                new
                {
                    date = D(Today),
                    totalLiters = 200,
                    pricePerLiter = 1.5,
                }
            )
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<MilkLogDto>(JsonOptions);
        dto!.PricePerLiter.ShouldBe(1.5m);
        dto.RevenueTotal.ShouldBe(300m);
    }

    [Test]
    public async Task Post_WithoutPricePerLiter_RevenueTotalIsNull()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 100 })
        );

        var dto = await response.Content.ReadFromJsonAsync<MilkLogDto>(JsonOptions);
        dto!.PricePerLiter.ShouldBeNull();
        dto.RevenueTotal.ShouldBeNull();
    }

    [Test]
    public async Task Post_NotesArePersisted()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(
                new
                {
                    date = D(Today),
                    totalLiters = 100,
                    notes = "rainy day",
                }
            )
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var dto = await response.Content.ReadFromJsonAsync<MilkLogDto>(JsonOptions);
        dto!.Notes.ShouldBe("rainy day");
    }

    // --- POST: validation ---

    [Test]
    public async Task Post_FutureDate_Returns400()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today.AddDays(1)), totalLiters = 100 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Post_DateMoreThan30DaysAgo_Returns400()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today.AddDays(-31)), totalLiters = 100 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Post_NegativeLiters_Returns400()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = -1 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Post_LitersAboveMax_Returns400()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 100000 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Post_NegativePricePerLiter_Returns400()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(
                new
                {
                    date = D(Today),
                    totalLiters = 100,
                    pricePerLiter = -0.01,
                }
            )
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Post_PriceAboveMax_Returns400()
    {
        var (farm, user) = await SetupFarmAsync();
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(
                new
                {
                    date = D(Today),
                    totalLiters = 100,
                    pricePerLiter = 10000,
                }
            )
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    // --- POST: authorization ---

    [Test]
    public async Task Post_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsync(
            "/api/farms/1/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 100 })
        );
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task Post_ViewerRole_Returns403()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 100 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task Post_AdminRole_Returns201()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Admin);
        Authenticate(user);

        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 100 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
    }

    [Test]
    public async Task Post_NonMember_Returns403()
    {
        var (farm, _) = await SetupFarmAsync();

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
        var response = await Client.PostAsync(
            $"/api/farms/{farm.Id}/milk-logs",
            JsonBody(new { date = D(Today), totalLiters = 100 })
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // --- GET list ---

    [Test]
    public async Task GetList_NoLogs_ReturnsEmptyLogsAndNullLastPrice()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result.ShouldNotBeNull();
        result!.Items.ShouldBeEmpty();
        result.LastUsedPricePerLiter.ShouldBeNull();
    }

    [Test]
    public async Task GetList_WithLogs_ReturnsAllLogsInRange()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 100m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-1), 200m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.Items.Count().ShouldBe(2);
    }

    [Test]
    public async Task GetList_LogOutsideDateRange_IsExcluded()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 100m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-31), 999m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.Items.Count().ShouldBe(1);
        result.Items.Single().TotalLiters.ShouldBe(100m);
    }

    [Test]
    public async Task GetList_ExplicitFromTo_FiltersCorrectly()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        var from = Today.AddDays(-5);
        var to = Today.AddDays(-3);

        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-4), 100m);
        await SeedLogAsync(farm.Id, user.Id, Today, 200m);

        Authenticate(user);
        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/milk-logs?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}"
        );

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.Items.Count().ShouldBe(1);
        result.Items.Single().TotalLiters.ShouldBe(100m);
    }

    [Test]
    public async Task GetList_LastUsedPricePerLiter_ReturnsLatestPricedLog()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-2), 100m, 1.25m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-1), 200m, 1.50m);
        await SeedLogAsync(farm.Id, user.Id, Today, 300m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.LastUsedPricePerLiter.ShouldBe(1.50m);
    }

    [Test]
    public async Task GetList_AllLogsUnpriced_LastUsedPriceIsNull()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 100m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.LastUsedPricePerLiter.ShouldBeNull();
    }

    [Test]
    public async Task GetList_RevenueTotalComputedForPricedLogs()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 200m, 1.5m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.Items.Single().RevenueTotal.ShouldBe(300m);
    }

    // --- GET list: pagination ---

    [Test]
    public async Task GetList_DefaultPage_ReturnsPaginationMetadata()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 100m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-1), 200m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.TotalCount.ShouldBe(2);
        result.Page.ShouldBe(1);
        result.PageSize.ShouldBe(30);
        result.TotalPages.ShouldBe(1);
    }

    [Test]
    public async Task GetList_PageSizeOne_ReturnsOnlyFirstRecord()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 111m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-1), 222m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-2), 333m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs?pageSize=1&page=1");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.Items.Count().ShouldBe(1);
        result.Items.Single().TotalLiters.ShouldBe(111m); // most recent first
        result.TotalCount.ShouldBe(3);
        result.TotalPages.ShouldBe(3);
    }

    [Test]
    public async Task GetList_SecondPage_ReturnsCorrectRecord()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 111m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-1), 222m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-2), 333m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs?pageSize=1&page=2");

        var result = await response.Content.ReadFromJsonAsync<MilkLogsListDto>(JsonOptions);
        result!.Items.Count().ShouldBe(1);
        result.Items.Single().TotalLiters.ShouldBe(222m);
        result.Page.ShouldBe(2);
    }

    [Test]
    public async Task GetList_ViewerCanRead()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [Test]
    public async Task GetList_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/farms/1/milk-logs");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // --- GET by date ---

    [Test]
    public async Task GetByDate_LogExists_Returns200WithDto()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 350m, 2m, "morning session");
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs/{Today:yyyy-MM-dd}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<MilkLogDto>(JsonOptions);
        dto.ShouldNotBeNull();
        dto!.TotalLiters.ShouldBe(350m);
        dto.PricePerLiter.ShouldBe(2m);
        dto.RevenueTotal.ShouldBe(700m);
        dto.Notes.ShouldBe("morning session");
    }

    [Test]
    public async Task GetByDate_NoLog_Returns404()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs/{Today:yyyy-MM-dd}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetByDate_DateBelongsToAnotherFarm_Returns404()
    {
        var (farm1, user1) = await SetupFarmAsync();
        var (farm2, user2) = await SetupFarmAsync();
        await SeedLogAsync(farm2.Id, user2.Id, Today);

        Authenticate(user1);
        var response = await Client.GetAsync($"/api/farms/{farm1.Id}/milk-logs/{Today:yyyy-MM-dd}");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetByDate_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync($"/api/farms/1/milk-logs/{Today:yyyy-MM-dd}");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // --- GET summary ---

    [Test]
    public async Task GetSummary_NoLogs_ReturnsZeroTotals()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MilkLogsSummaryDto>(JsonOptions);
        result.ShouldNotBeNull();
        result!.TotalLiters.ShouldBe(0m);
        result.TotalRevenue.ShouldBe(0m);
        result.DaysLogged.ShouldBe(0);
    }

    [Test]
    public async Task GetSummary_WithLogs_ReturnsCorrectAggregates()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today, 100m, 1.5m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-1), 200m, 2m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-2), 50m);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs/summary");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MilkLogsSummaryDto>(JsonOptions);
        result.ShouldNotBeNull();
        result!.TotalLiters.ShouldBe(350m);
        result.TotalRevenue.ShouldBe(550m);
        result.DaysLogged.ShouldBe(3);
    }

    [Test]
    public async Task GetSummary_DateRange_ExcludesLogsOutsideRange()
    {
        var (farm, user) = await SetupFarmAsync(FarmMemberRoles.Viewer);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-5), 120m, 1.25m);
        await SeedLogAsync(farm.Id, user.Id, Today.AddDays(-40), 900m, 2m);
        Authenticate(user);

        var from = D(Today.AddDays(-7));
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/milk-logs/summary?from={from}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<MilkLogsSummaryDto>(JsonOptions);
        result.ShouldNotBeNull();
        result!.TotalLiters.ShouldBe(120m);
        result.TotalRevenue.ShouldBe(150m);
        result.DaysLogged.ShouldBe(1);
    }

    [Test]
    public async Task GetSummary_Unauthenticated_Returns401()
    {
        var response = await Client.GetAsync("/api/farms/1/milk-logs/summary");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
