using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Animals;

public class AnimalRetirementIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private async Task<(Farm farm, Animal animal, User user)> SetupFarmWithAnimalAndMemberAsync(
        string role
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
        await DbContext.SaveChangesAsync();

        var lot = new Lot
        {
            Name = "L1",
            PaddockId = paddock.Id,
            Status = "Active",
        };
        DbContext.Lots.Add(lot);
        await DbContext.SaveChangesAsync();

        var animal = new Animal
        {
            Name = "Bessie",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-3),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        DbContext.Animals.Add(animal);

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = role,
            }
        );
        await DbContext.SaveChangesAsync();

        return (farm, animal, user);
    }

    [Test]
    public async Task Retire_Sold_SetsLifeStatusSold()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var request = new RetireAnimalRequest
        {
            Reason = RetirementReason.Sold,
            At = DateTime.UtcNow,
            SalePrice = 1500.00m,
            Notes = "Sold to neighbor",
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/retire",
            request,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AnimalRetirementDto>(JsonOptions);
        dto.ShouldNotBeNull();
        dto.AnimalId.ShouldBe(animal.Id);
        dto.Reason.ShouldBe(RetirementReason.Sold);
        dto.SalePrice.ShouldBe(1500.00m);
        dto.Notes.ShouldBe("Sold to neighbor");
        dto.UserName.ShouldBe(user.Name);

        DbContext.ChangeTracker.Clear();
        var updatedAnimal = await DbContext.Animals.FindAsync(animal.Id);
        updatedAnimal!.LifeStatus.ShouldBe(LifeStatus.Sold);
    }

    [Test]
    public async Task Retire_Dead_SetsLifeStatusDead()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var request = new RetireAnimalRequest
        {
            Reason = RetirementReason.Dead,
            At = DateTime.UtcNow,
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/retire",
            request,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DbContext.ChangeTracker.Clear();
        var updatedAnimal = await DbContext.Animals.FindAsync(animal.Id);
        updatedAnimal!.LifeStatus.ShouldBe(LifeStatus.Dead);
    }

    [Test]
    public async Task Retire_Stolen_SetsLifeStatusMissing()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var request = new RetireAnimalRequest
        {
            Reason = RetirementReason.Stolen,
            At = DateTime.UtcNow,
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/retire",
            request,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DbContext.ChangeTracker.Clear();
        var updatedAnimal = await DbContext.Animals.FindAsync(animal.Id);
        updatedAnimal!.LifeStatus.ShouldBe(LifeStatus.Missing);
    }

    [Test]
    public async Task Retire_Other_SetsLifeStatusRetired()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var request = new RetireAnimalRequest
        {
            Reason = RetirementReason.Other,
            At = DateTime.UtcNow,
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/retire",
            request,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        DbContext.ChangeTracker.Clear();
        var updatedAnimal = await DbContext.Animals.FindAsync(animal.Id);
        updatedAnimal!.LifeStatus.ShouldBe(LifeStatus.Retired);
    }

    [Test]
    public async Task Retire_AsViewer_ShouldReturnForbidden()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var request = new RetireAnimalRequest
        {
            Reason = RetirementReason.Dead,
            At = DateTime.UtcNow,
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/retire",
            request,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [TestCase(LifeStatus.Sold)]
    [TestCase(LifeStatus.Dead)]
    [TestCase(LifeStatus.Retired)]
    [TestCase(LifeStatus.Missing)]
    public async Task Retire_NonActiveAnimal_ShouldReturnConflict(LifeStatus status)
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);

        animal.LifeStatus = status;
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var request = new RetireAnimalRequest
        {
            Reason = RetirementReason.Other,
            At = DateTime.UtcNow,
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/retire",
            request,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Conflict);
    }

    [Test]
    public async Task Retire_NonExistentAnimal_ShouldReturnNotFound()
    {
        var (farm, _, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var request = new RetireAnimalRequest
        {
            Reason = RetirementReason.Sold,
            At = DateTime.UtcNow,
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/99999/retire",
            request,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Search_Default_ExcludesSoldDeadRetiredAnimals()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);

        animal.LifeStatus = LifeStatus.Sold;
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/search");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<AnimalListDto>>(
            JsonOptions
        );
        result.ShouldNotBeNull();
        result.Items.ShouldNotContain(a => a.Id == animal.Id);
    }

    [Test]
    public async Task Search_Default_IncludesMissingAnimals()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);

        animal.LifeStatus = LifeStatus.Missing;
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/search");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<AnimalListDto>>(
            JsonOptions
        );
        result.ShouldNotBeNull();
        result.Items.ShouldContain(a => a.Id == animal.Id);
    }

    [Test]
    public async Task Search_IncludeRetiredTrue_ReturnsAllAnimals()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);

        animal.LifeStatus = LifeStatus.Retired;
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/search?includeRetired=true"
        );
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var result = await response.Content.ReadFromJsonAsync<PagedResultDto<AnimalListDto>>(
            JsonOptions
        );
        result.ShouldNotBeNull();
        result.Items.ShouldContain(a => a.Id == animal.Id);
    }

    [Test]
    public async Task GetTimeline_AfterRetirement_IncludesRetirementEvent()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);

        var retirementAt = DateTime.UtcNow.AddDays(-1);
        var retirement = new AnimalRetirement
        {
            AnimalId = animal.Id,
            UserId = user.Id,
            Reason = RetirementReason.Dead,
            At = retirementAt,
            Notes = "Found deceased",
            CreatedAt = DateTime.UtcNow,
        };
        DbContext.AnimalRetirements.Add(retirement);
        animal.LifeStatus = LifeStatus.Dead;
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/{animal.Id}/timeline");
        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var timeline = await response.Content.ReadFromJsonAsync<IEnumerable<AnimalTimelineItemDto>>(
            JsonOptions
        );
        timeline.ShouldNotBeNull();

        var retirementItem = timeline.FirstOrDefault(i => i.Type == "retirement");
        retirementItem.ShouldNotBeNull();
        retirementItem.Retirement.ShouldNotBeNull();
        retirementItem.Retirement!.Reason.ShouldBe(RetirementReason.Dead);
        retirementItem.Retirement.Notes.ShouldBe("Found deceased");
    }
}

internal sealed class PagedResultDto<T>
{
    public IEnumerable<T> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
}
