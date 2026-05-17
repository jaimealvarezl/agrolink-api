using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Animals;

public class AnimalBcsReadingsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private async Task<(Farm farm, Animal animal, User user)> SetupAsync(string role)
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
    public async Task CreateBcsReading_AsEditor_Returns201WithCorrectData()
    {
        var (farm, animal, user) = await SetupAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var dto = new CreateBcsReadingDto
        {
            Score = 3.0,
            Source = BcsReadingSource.AI,
            HasAlerts = false,
            RawAiResponse = "{\"bcs\": 3.0}",
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/bcs-readings",
            dto,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var result = await response.Content.ReadFromJsonAsync<AnimalBcsReadingDto>(JsonOptions);
        result.ShouldNotBeNull();
        result!.AnimalId.ShouldBe(animal.Id);
        result.Score.ShouldBe(3.0);
        result.Source.ShouldBe(BcsReadingSource.AI);
        result.ConfirmedByUserId.ShouldBe(user.Id);

        // Row persisted
        DbContext.ChangeTracker.Clear();
        var readings = DbContext.AnimalBcsReadings.Where(r => r.AnimalId == animal.Id).ToList();
        readings.Count.ShouldBe(1);
        readings[0].Score.ShouldBe(3.0);
        readings[0].RawAiResponse.ShouldBe("{\"bcs\": 3.0}");
    }

    [Test]
    public async Task CreateBcsReading_NoAlerts_CreatesOnlyBcsNote()
    {
        var (farm, animal, user) = await SetupAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var dto = new CreateBcsReadingDto
        {
            Score = 2.5,
            Source = BcsReadingSource.AI,
            HasAlerts = false,
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/bcs-readings",
            dto,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        DbContext.ChangeTracker.Clear();
        var notes = DbContext
            .AnimalNotes.Where(n => n.AnimalId == animal.Id)
            .OrderBy(n => n.CreatedAt)
            .ToList();

        notes.Count.ShouldBe(1);
        notes[0].Content.ShouldContain("CC 2.5");
        notes[0].Content.ShouldContain(user.Name);
        notes[0].Content.ShouldContain("Análisis IA confirmado por");
    }

    [Test]
    public async Task CreateBcsReading_WithAlerts_CreatesBothNotes()
    {
        var (farm, animal, user) = await SetupAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var dto = new CreateBcsReadingDto
        {
            Score = 1.5,
            Source = BcsReadingSource.AI,
            HasAlerts = true,
            AlertDescription = "Garrapatas visibles en lomo y oreja derecha.",
        };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/bcs-readings",
            dto,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);

        DbContext.ChangeTracker.Clear();
        var notes = DbContext.AnimalNotes.Where(n => n.AnimalId == animal.Id).ToList();

        notes.Count.ShouldBe(2);
        notes.ShouldContain(n => n.Content.StartsWith("CC 1.5"));
        notes.ShouldContain(n =>
            n.Content == "Alerta IA: Garrapatas visibles en lomo y oreja derecha."
        );
    }

    [Test]
    public async Task CreateBcsReading_AsViewer_Returns403()
    {
        var (farm, animal, user) = await SetupAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var dto = new CreateBcsReadingDto { Score = 3.0, Source = BcsReadingSource.Manual };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/bcs-readings",
            dto,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CreateBcsReading_Unauthenticated_Returns401()
    {
        var response = await Client.PostAsJsonAsync(
            "/api/farms/1/animals/1/bcs-readings",
            new CreateBcsReadingDto { Score = 3.0, Source = BcsReadingSource.AI },
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task CreateBcsReading_ScoreOutOfRange_Returns400()
    {
        var (farm, animal, user) = await SetupAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var dto = new CreateBcsReadingDto { Score = 9.1, Source = BcsReadingSource.Manual };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/bcs-readings",
            dto,
            JsonOptions
        );

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
