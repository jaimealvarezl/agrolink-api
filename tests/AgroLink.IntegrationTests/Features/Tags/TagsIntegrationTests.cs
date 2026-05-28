using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Tags.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Tags;

public class TagsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Test]
    public async Task GetFarmTags_ShouldReturnUsageCountsSorted()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Viewer, "list");

        var tagA = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "venta",
            DisplayName = "Venta",
            CreatedByUserId = user.Id,
        };
        var tagB = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "cuarentena",
            DisplayName = "Cuarentena",
            CreatedByUserId = user.Id,
        };
        DbContext.Tags.AddRange(tagA, tagB);

        var animal1 = CreateAnimal(lot.Id, "A1");
        var animal2 = CreateAnimal(lot.Id, "A2");
        DbContext.Animals.AddRange(animal1, animal2);
        await DbContext.SaveChangesAsync();

        DbContext.AnimalTags.AddRange(
            new AnimalTag
            {
                AnimalId = animal1.Id,
                TagId = tagA.Id,
                AddedByUserId = user.Id,
            },
            new AnimalTag
            {
                AnimalId = animal2.Id,
                TagId = tagA.Id,
                AddedByUserId = user.Id,
            },
            new AnimalTag
            {
                AnimalId = animal2.Id,
                TagId = tagB.Id,
                AddedByUserId = user.Id,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/tags");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var tags = await response.Content.ReadFromJsonAsync<List<TagDto>>(JsonOptions);
        tags.ShouldNotBeNull();
        tags.Count.ShouldBe(2);
        tags[0].DisplayName.ShouldBe("Venta");
        tags[0].UsageCount.ShouldBe(2);
        tags[1].DisplayName.ShouldBe("Cuarentena");
        tags[1].UsageCount.ShouldBe(1);
    }

    [Test]
    public async Task RenameTag_ShouldUpdateDisplayNameOnly()
    {
        var (farm, _, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "rename");

        var tag = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "venta",
            DisplayName = "Venta",
            CreatedByUserId = user.Id,
        };
        DbContext.Tags.Add(tag);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.PutAsJsonAsync(
            $"/api/farms/{farm.Id}/tags/{tag.Id}",
            new { displayName = "Para Venta" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<TagDto>(JsonOptions);
        dto.ShouldNotBeNull();
        dto.DisplayName.ShouldBe("Para Venta");

        DbContext.ChangeTracker.Clear();
        var updatedTag = await DbContext.Tags.FirstAsync(t => t.Id == tag.Id);
        updatedTag.DisplayName.ShouldBe("Para Venta");
        updatedTag.CanonicalName.ShouldBe("venta");
    }

    [Test]
    public async Task Rename_WhenTagBelongsToDifferentFarm_ShouldReturnNotFound()
    {
        var (farmA, _, userA) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "farm_a_rename");
        var (farmB, _, userB) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "farm_b_rename");

        var tagInFarmB = new Tag
        {
            FarmId = farmB.Id,
            CanonicalName = "venta",
            DisplayName = "Venta",
            CreatedByUserId = userB.Id,
        };
        DbContext.Tags.Add(tagInFarmB);
        await DbContext.SaveChangesAsync();

        Authenticate(userA);

        var response = await Client.PutAsJsonAsync(
            $"/api/farms/{farmA.Id}/tags/{tagInFarmB.Id}",
            new { displayName = "Para Venta" }
        );

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task DeleteTag_ShouldCascadeDetachAnimalsAndNotDeleteAnimals()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "delete");

        var tag = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "venta",
            DisplayName = "Venta",
            CreatedByUserId = user.Id,
        };
        DbContext.Tags.Add(tag);

        var animal1 = CreateAnimal(lot.Id, "D1");
        var animal2 = CreateAnimal(lot.Id, "D2");
        DbContext.Animals.AddRange(animal1, animal2);
        await DbContext.SaveChangesAsync();

        DbContext.AnimalTags.AddRange(
            new AnimalTag
            {
                AnimalId = animal1.Id,
                TagId = tag.Id,
                AddedByUserId = user.Id,
            },
            new AnimalTag
            {
                AnimalId = animal2.Id,
                TagId = tag.Id,
                AddedByUserId = user.Id,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var response = await Client.DeleteAsync($"/api/farms/{farm.Id}/tags/{tag.Id}");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var payload = await response.Content.ReadFromJsonAsync<JsonElement>();
        payload.GetProperty("affectedAnimals").GetInt32().ShouldBe(2);

        DbContext.ChangeTracker.Clear();
        (await DbContext.Tags.AnyAsync(t => t.Id == tag.Id)).ShouldBeFalse();
        (await DbContext.AnimalTags.AnyAsync(at => at.TagId == tag.Id)).ShouldBeFalse();
        (await DbContext.Animals.CountAsync()).ShouldBe(2);
    }

    [Test]
    public async Task ForemanEditor_CreateAnimalWithNewTag_ShouldReturnForbidden()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Editor, "foreman");
        Authenticate(user);

        var createDto = new CreateAnimalDto
        {
            Name = "Cow Foreman",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-1),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = new List<AnimalOwnerCreateDto>(),
            Tags = ["TagNueva"],
        };

        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/animals", createDto);

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task CanonicalDedupe_CreateVentaThenventa_ShouldReuseSameTag()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "dedupe");
        Authenticate(user);

        var firstAnimalResponse = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals",
            new CreateAnimalDto
            {
                Name = "Cow 1",
                Sex = Sex.Female,
                LotId = lot.Id,
                BirthDate = DateTime.UtcNow.AddYears(-1),
                LifeStatus = LifeStatus.Active,
                ProductionStatus = ProductionStatus.Calf,
                HealthStatus = HealthStatus.Healthy,
                ReproductiveStatus = ReproductiveStatus.NotApplicable,
                Owners = new List<AnimalOwnerCreateDto>(),
                Tags = ["Venta"],
            }
        );

        var secondAnimalResponse = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals",
            new CreateAnimalDto
            {
                Name = "Cow 2",
                Sex = Sex.Female,
                LotId = lot.Id,
                BirthDate = DateTime.UtcNow.AddYears(-1),
                LifeStatus = LifeStatus.Active,
                ProductionStatus = ProductionStatus.Calf,
                HealthStatus = HealthStatus.Healthy,
                ReproductiveStatus = ReproductiveStatus.NotApplicable,
                Owners = new List<AnimalOwnerCreateDto>(),
                Tags = ["venta"],
            }
        );

        firstAnimalResponse.StatusCode.ShouldBe(HttpStatusCode.Created);
        secondAnimalResponse.StatusCode.ShouldBe(HttpStatusCode.Created);

        var tags = await DbContext.Tags.Where(t => t.FarmId == farm.Id).ToListAsync();
        tags.Count.ShouldBe(1);
        tags[0].CanonicalName.ShouldBe("venta");

        var animalTags = await DbContext
            .AnimalTags.Where(at => at.TagId == tags[0].Id)
            .ToListAsync();
        animalTags.Count.ShouldBe(2);
    }

    private static Animal CreateAnimal(int lotId, string name)
    {
        return new Animal
        {
            Name = name,
            Sex = Sex.Female,
            LotId = lotId,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
    }

    private async Task<(Farm farm, Lot lot, User user)> SetupFarmWithLotAsync(
        string role,
        string suffix
    )
    {
        var user = new User
        {
            Name = $"User {suffix}",
            Email = $"user_{suffix}@tags.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = $"Owner {suffix}", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm
        {
            Name = $"Farm {suffix}",
            Location = "Location",
            OwnerId = owner.Id,
        };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        owner.FarmId = farm.Id;
        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = role,
            }
        );

        var paddock = new Paddock { Name = $"Paddock {suffix}", FarmId = farm.Id };
        DbContext.Paddocks.Add(paddock);
        await DbContext.SaveChangesAsync();

        var lot = new Lot
        {
            Name = $"Lot {suffix}",
            PaddockId = paddock.Id,
            Status = "Active",
        };
        DbContext.Lots.Add(lot);
        await DbContext.SaveChangesAsync();

        return (farm, lot, user);
    }
}
