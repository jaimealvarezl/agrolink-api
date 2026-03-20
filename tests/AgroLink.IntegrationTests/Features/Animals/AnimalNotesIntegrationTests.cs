using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Animals;

public class AnimalNotesIntegrationTests : IntegrationTestBase
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
    public async Task GetNotes_AsViewer_ShouldReturnOk()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/{animal.Id}/notes");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var notes = await response.Content.ReadFromJsonAsync<IEnumerable<AnimalNoteDto>>(
            JsonOptions
        );
        notes.ShouldNotBeNull();
        notes.ShouldBeEmpty();
    }

    [Test]
    public async Task CreateNote_AsEditor_ShouldReturnCreatedWithCorrectData()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);
        Authenticate(user);

        var dto = new CreateAnimalNoteDto { Content = "Animal is healthy after checkup." };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/notes",
            dto
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AnimalNoteDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.AnimalId.ShouldBe(animal.Id);
        created.Content.ShouldBe("Animal is healthy after checkup.");
        created.UserName.ShouldBe(user.Name);

        // Verify it's persisted
        DbContext.ChangeTracker.Clear();
        var notesInDb = DbContext.AnimalNotes.Where(n => n.AnimalId == animal.Id).ToList();
        notesInDb.Count.ShouldBe(1);
    }

    [Test]
    public async Task CreateNote_AsViewer_ShouldReturnForbidden()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Viewer);
        Authenticate(user);

        var dto = new CreateAnimalNoteDto { Content = "Viewer trying to add note." };

        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/notes",
            dto
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task DeleteNote_AsAuthor_ShouldReturnNoContent()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);

        var note = new AnimalNote
        {
            AnimalId = animal.Id,
            Content = "To be deleted",
            UserId = user.Id,
        };
        DbContext.AnimalNotes.Add(note);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/notes/{note.Id}"
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext.AnimalNotes.FindAsync(note.Id);
        deleted.ShouldBeNull();
    }

    [Test]
    public async Task DeleteNote_ByNonAuthor_ShouldReturnForbidden()
    {
        // Two editors: one creates the note, other tries to delete it
        var (farm, animal, author) = await SetupFarmWithAnimalAndMemberAsync(
            FarmMemberRoles.Editor
        );

        var otherEditor = new User
        {
            Name = "Other Editor",
            Email = "other@test.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(otherEditor);
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = otherEditor.Id,
                Role = FarmMemberRoles.Editor,
            }
        );

        var note = new AnimalNote
        {
            AnimalId = animal.Id,
            Content = "Author's note",
            UserId = author.Id,
        };
        DbContext.AnimalNotes.Add(note);
        await DbContext.SaveChangesAsync();

        Authenticate(otherEditor); // log in as the other editor

        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/notes/{note.Id}"
        );

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetTimeline_ShouldReturnAllEventTypesSortedByDate()
    {
        var (farm, animal, user) = await SetupFarmWithAnimalAndMemberAsync(FarmMemberRoles.Editor);

        var note = new AnimalNote
        {
            AnimalId = animal.Id,
            Content = "Timeline note",
            UserId = user.Id,
            CreatedAt = DateTime.UtcNow,
        };
        DbContext.AnimalNotes.Add(note);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/{animal.Id}/timeline");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var timeline = await response.Content.ReadFromJsonAsync<IEnumerable<AnimalTimelineItemDto>>(
            JsonOptions
        );
        timeline.ShouldNotBeNull();
        var items = timeline.ToList();
        items.ShouldNotBeEmpty();

        var noteItem = items.FirstOrDefault(i => i.Type == "note");
        noteItem.ShouldNotBeNull();
        noteItem.Note.ShouldNotBeNull();
        noteItem.Note!.Content.ShouldBe("Timeline note");

        // Items should be sorted newest first
        for (var i = 1; i < items.Count; i++)
        {
            items[i - 1].OccurredAt.ShouldBeGreaterThanOrEqualTo(items[i].OccurredAt);
        }
    }

    [Test]
    public async Task GetNotes_Unauthenticated_ShouldReturnUnauthorized()
    {
        // Don't call Authenticate
        var response = await Client.GetAsync("/api/farms/1/animals/1/notes");
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }
}
