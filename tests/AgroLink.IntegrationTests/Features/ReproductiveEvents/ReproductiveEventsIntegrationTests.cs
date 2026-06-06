using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.ReproductiveEvents.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.ReproductiveEvents;

public class ReproductiveEventsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Test]
    public async Task PostAndGet_HappyPath_ShouldCreateAndReturnEvent()
    {
        var (farm, female, _, user) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var createDto = new CreateReproductiveEventDto
        {
            EventType = ReproductiveEventType.Heat,
            Date = DateTime.UtcNow.Date.AddDays(-1),
            Status = ReproductiveEventStatus.Positive,
        };

        var postResponse = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events",
            createDto,
            JsonOptions
        );

        postResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await postResponse.Content.ReadFromJsonAsync<ReproductiveEventDto>(
            JsonOptions
        );
        created.ShouldNotBeNull();

        var getResponse = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events/{created!.Id}"
        );
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var byId = await getResponse.Content.ReadFromJsonAsync<ReproductiveEventDto>(JsonOptions);
        byId.ShouldNotBeNull();
        byId!.Id.ShouldBe(created.Id);
        byId.EventType.ShouldBe(ReproductiveEventType.Heat);

        var listResponse = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events"
        );
        listResponse.StatusCode.ShouldBe(HttpStatusCode.OK);
        var list = await listResponse.Content.ReadFromJsonAsync<
            IReadOnlyList<ReproductiveEventDto>
        >(JsonOptions);
        list.ShouldNotBeNull();
        list!.Count.ShouldBe(1);
    }

    [Test]
    public async Task ViewerCanGetButNotPost_AndNonMemberForbidden()
    {
        var (farm, female, _, viewer) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Viewer);
        Authenticate(viewer);

        var getResponse = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events"
        );
        getResponse.StatusCode.ShouldBe(HttpStatusCode.OK);

        var postResponse = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events",
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Heat,
                Date = DateTime.UtcNow.Date,
            },
            JsonOptions
        );
        postResponse.StatusCode.ShouldBe(HttpStatusCode.Forbidden);

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
        var outsiderGet = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events"
        );
        outsiderGet.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task MatingPositive_ShouldPersistExpectedDueDate()
    {
        var (farm, female, bull, user) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var date = new DateTime(2026, 5, 1);
        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events",
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Mating,
                Date = date,
                Status = ReproductiveEventStatus.Positive,
                BullId = bull.Id,
            },
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        DbContext.ChangeTracker.Clear();
        var ev = await DbContext.ReproductiveEvents.SingleAsync();
        ev.ExpectedDueDate.ShouldBe(date.AddDays(283));
    }

    [Test]
    public async Task PregnancyCheckPositive_ShouldFlipAnimalStatusToPregnant()
    {
        var (farm, female, _, user) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Editor);
        female.ReproductiveStatus = ReproductiveStatus.Open;
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events",
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date,
                Status = ReproductiveEventStatus.Positive,
                EstimatedMonths = 3,
            },
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        DbContext.ChangeTracker.Clear();
        var reloaded = await DbContext.Animals.SingleAsync(a => a.Id == female.Id);
        reloaded.ReproductiveStatus.ShouldBe(ReproductiveStatus.Pregnant);
    }

    [Test]
    public async Task PregnancyCheckNegative_ShouldFlipAnimalStatusToOpen()
    {
        var (farm, female, _, user) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Editor);
        female.ReproductiveStatus = ReproductiveStatus.Pregnant;
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events",
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date,
                Status = ReproductiveEventStatus.Negative,
            },
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        DbContext.ChangeTracker.Clear();
        var reloaded = await DbContext.Animals.SingleAsync(a => a.Id == female.Id);
        reloaded.ReproductiveStatus.ShouldBe(ReproductiveStatus.Open);
    }

    [Test]
    public async Task BackdatedNegativeWithLaterPositive_ShouldNotFlipStatus()
    {
        var (farm, female, _, user) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Editor);
        female.ReproductiveStatus = ReproductiveStatus.Pregnant;

        DbContext.ReproductiveEvents.Add(
            new ReproductiveEvent
            {
                AnimalId = female.Id,
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date.AddDays(-2),
                Status = ReproductiveEventStatus.Positive,
                EstimatedMonths = 4,
                CreatedByUserId = user.Id,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events",
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.PregnancyCheck,
                Date = DateTime.UtcNow.Date.AddDays(-10),
                Status = ReproductiveEventStatus.Negative,
            },
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        DbContext.ChangeTracker.Clear();
        var reloaded = await DbContext.Animals.SingleAsync(a => a.Id == female.Id);
        reloaded.ReproductiveStatus.ShouldBe(ReproductiveStatus.Pregnant);

        var events = await DbContext
            .ReproductiveEvents.Where(e => e.AnimalId == female.Id)
            .ToListAsync();
        events.Count.ShouldBe(2);
    }

    [Test]
    public async Task PostOnMaleAnimal_ShouldReturnBadRequest()
    {
        var (farm, _, _, user) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Editor);
        var male = new Animal
        {
            Name = "Male Target",
            Sex = Sex.Male,
            LotId = DbContext.Lots.First().Id,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        DbContext.Animals.Add(male);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{male.Id}/reproductive-events",
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Heat,
                Date = DateTime.UtcNow.Date,
            },
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task PostWithFemaleBull_ShouldReturnBadRequest()
    {
        var (farm, female, _, user) = await SetupFarmWithAnimalsAsync(FarmMemberRoles.Editor);

        var femaleBull = new Animal
        {
            Name = "Female Bull",
            Sex = Sex.Female,
            LotId = female.LotId,
            BirthDate = DateTime.UtcNow.AddYears(-3),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.Open,
        };
        DbContext.Animals.Add(femaleBull);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{female.Id}/reproductive-events",
            new CreateReproductiveEventDto
            {
                EventType = ReproductiveEventType.Mating,
                Date = DateTime.UtcNow.Date,
                BullId = femaleBull.Id,
            },
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    private async Task<(
        Farm farm,
        Animal female,
        Animal bull,
        User user
    )> SetupFarmWithAnimalsAsync(string role)
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

        var female = new Animal
        {
            Name = "Bessie",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-3),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.Open,
        };

        var bull = new Animal
        {
            Name = "Toro",
            Sex = Sex.Male,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-4),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };

        DbContext.Animals.AddRange(female, bull);

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = role,
            }
        );

        await DbContext.SaveChangesAsync();

        return (farm, female, bull, user);
    }
}
