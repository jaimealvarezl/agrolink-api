using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Animals;

public class AnimalsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Test]
    public async Task GetAll_WhenUserIsMember_ShouldReturnOk()
    {
        // Arrange
        var user = new User
        {
            Name = "Test User",
            Email = "test@example.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Main Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm
        {
            Name = "Test Farm",
            Location = "Location",
            OwnerId = owner.Id,
        };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        var member = new FarmMember
        {
            FarmId = farm.Id,
            UserId = user.Id,
            Role = FarmMemberRoles.Viewer,
        };
        DbContext.FarmMembers.Add(member);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var animals = await response.Content.ReadFromJsonAsync<IEnumerable<AnimalDto>>(JsonOptions);
        animals.ShouldNotBeNull();
    }

    [Test]
    public async Task GetById_WhenAnimalExists_ShouldReturnOk()
    {
        // Arrange
        var user = new User
        {
            Name = "Viewer",
            Email = "viewer@ani.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Farm", OwnerId = owner.Id };
        var paddock = new Paddock { Name = "P1", FarmId = 0 };
        var lot = new Lot
        {
            Name = "L1",
            PaddockId = 0,
            Status = "Active",
        };
        paddock.Lots.Add(lot);
        farm.Paddocks.Add(paddock);
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        var animal = new Animal
        {
            Name = "Cow 1",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-2),
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
                Role = FarmMemberRoles.Viewer,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/{animal.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var dto = await response.Content.ReadFromJsonAsync<AnimalDto>(JsonOptions);
        dto.ShouldNotBeNull();
        dto.Id.ShouldBe(animal.Id);
        dto.Name.ShouldBe(animal.Name);
    }

    [Test]
    public async Task Create_WhenUserIsEditor_ShouldReturnCreated()
    {
        // Arrange
        var user = new User
        {
            Name = "Editor User",
            Email = "editor@example.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner 3", Phone = "789" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm
        {
            Name = "Work Farm",
            Location = "Here",
            OwnerId = owner.Id,
        };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        owner.FarmId = farm.Id;
        await DbContext.SaveChangesAsync();

        var member = new FarmMember
        {
            FarmId = farm.Id,
            UserId = user.Id,
            Role = FarmMemberRoles.Editor,
        };
        DbContext.FarmMembers.Add(member);

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

        Authenticate(user);

        var createDto = new CreateAnimalDto
        {
            Name = "New Cow",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-1),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = new List<AnimalOwnerCreateDto>(),
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/animals", createDto);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AnimalDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.Name.ShouldBe("New Cow");
    }

    [Test]
    public async Task Search_WhenValid_ShouldReturnPagedResult()
    {
        // Arrange
        var user = new User
        {
            Name = "Search User",
            Email = "search@example.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Farm", OwnerId = owner.Id };
        var paddock = new Paddock { Name = "P1", FarmId = 0 };
        var lot = new Lot
        {
            Name = "L1",
            PaddockId = 0,
            Status = "Active",
        };
        paddock.Lots.Add(lot);
        farm.Paddocks.Add(paddock);
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        var animal = new Animal
        {
            Name = "Cow 1",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-2),
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
                Role = FarmMemberRoles.Viewer,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/search?LotId={lot.Id}&PageSize=10&Page=1"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AnimalListDto>>(
            JsonOptions
        );
        result.ShouldNotBeNull();
        result.Items.ShouldNotBeEmpty();
        result.Items.First().Name.ShouldBe("Cow 1");
    }

    [Test]
    public async Task Create_WithNewTags_ShouldCreateTagRows()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "tags_new");
        Authenticate(user);

        var createDto = new CreateAnimalDto
        {
            Name = "Cow Tagged New",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-1),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = new List<AnimalOwnerCreateDto>(),
            Tags = ["Venta", "Premio"],
        };

        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/animals", createDto);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tags = await DbContext.Tags.Where(t => t.FarmId == farm.Id).ToListAsync();
        tags.Count.ShouldBe(2);
        tags.Select(t => t.CanonicalName).ShouldContain("venta");
        tags.Select(t => t.CanonicalName).ShouldContain("premio");
    }

    [Test]
    public async Task Create_WithExistingTagDifferentCase_ShouldReuseExistingTag()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "tags_case");
        Authenticate(user);

        var existingTag = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "venta",
            DisplayName = "Venta",
            CreatedByUserId = user.Id,
        };
        DbContext.Tags.Add(existingTag);
        await DbContext.SaveChangesAsync();

        var createDto = new CreateAnimalDto
        {
            Name = "Cow Tagged Existing",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-1),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
            Owners = new List<AnimalOwnerCreateDto>(),
            Tags = ["venta"],
        };

        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/animals", createDto);

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var tags = await DbContext.Tags.Where(t => t.FarmId == farm.Id).ToListAsync();
        tags.Count.ShouldBe(1);

        var createdAnimal = await response.Content.ReadFromJsonAsync<AnimalDto>(JsonOptions);
        createdAnimal.ShouldNotBeNull();

        var animalTag = await DbContext.AnimalTags.FirstOrDefaultAsync(at =>
            at.AnimalId == createdAnimal.Id
        );
        animalTag.ShouldNotBeNull();
        animalTag.TagId.ShouldBe(existingTag.Id);
    }

    [Test]
    public async Task Update_ReplacesTagSet()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Admin, "tags_update");
        Authenticate(user);

        var firstTag = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "venta",
            DisplayName = "Venta",
            CreatedByUserId = user.Id,
        };
        var secondTag = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "premio",
            DisplayName = "Premio",
            CreatedByUserId = user.Id,
        };
        DbContext.Tags.AddRange(firstTag, secondTag);
        await DbContext.SaveChangesAsync();

        var animal = new Animal
        {
            Name = "Cow Replace Tags",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        DbContext.Animals.Add(animal);
        await DbContext.SaveChangesAsync();

        DbContext.AnimalTags.Add(
            new AnimalTag
            {
                AnimalId = animal.Id,
                TagId = firstTag.Id,
                AddedByUserId = user.Id,
            }
        );
        await DbContext.SaveChangesAsync();

        var updateDto = new UpdateAnimalDto { Tags = ["Premio"] };
        var response = await Client.PutAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}",
            updateDto
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);

        var tagsForAnimal = await DbContext
            .AnimalTags.Where(at => at.AnimalId == animal.Id)
            .Select(at => at.TagId)
            .ToListAsync();

        tagsForAnimal.Count.ShouldBe(1);
        tagsForAnimal.Single().ShouldBe(secondTag.Id);
    }

    [Test]
    public async Task Search_WithTagIdsFilter_ShouldUseOrSemantics()
    {
        var (farm, lot, user) = await SetupFarmWithLotAsync(FarmMemberRoles.Viewer, "tags_filter");
        Authenticate(user);

        var ownerMembership = await DbContext.FarmMembers.FirstAsync(fm =>
            fm.FarmId == farm.Id && fm.UserId == user.Id
        );
        ownerMembership.Role = FarmMemberRoles.Admin;
        await DbContext.SaveChangesAsync();

        var saleTag = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "venta",
            DisplayName = "Venta",
            CreatedByUserId = user.Id,
        };
        var quarantineTag = new Tag
        {
            FarmId = farm.Id,
            CanonicalName = "cuarentena",
            DisplayName = "Cuarentena",
            CreatedByUserId = user.Id,
        };
        DbContext.Tags.AddRange(saleTag, quarantineTag);
        await DbContext.SaveChangesAsync();

        var animal1 = new Animal
        {
            Name = "Cow Sale",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        var animal2 = new Animal
        {
            Name = "Cow Quarantine",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        var animal3 = new Animal
        {
            Name = "Cow No Tag",
            Sex = Sex.Female,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        DbContext.Animals.AddRange(animal1, animal2, animal3);
        await DbContext.SaveChangesAsync();

        DbContext.AnimalTags.AddRange(
            new AnimalTag
            {
                AnimalId = animal1.Id,
                TagId = saleTag.Id,
                AddedByUserId = user.Id,
            },
            new AnimalTag
            {
                AnimalId = animal2.Id,
                TagId = quarantineTag.Id,
                AddedByUserId = user.Id,
            }
        );
        await DbContext.SaveChangesAsync();

        ownerMembership.Role = FarmMemberRoles.Viewer;
        await DbContext.SaveChangesAsync();

        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/search?tagIds={saleTag.Id}&tagIds={quarantineTag.Id}"
        );

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<PagedResult<AnimalListDto>>(
            JsonOptions
        );
        result.ShouldNotBeNull();
        result.Items.Count().ShouldBe(2);
        result.Items.Any(a => a.Name == "Cow Sale").ShouldBeTrue();
        result.Items.Any(a => a.Name == "Cow Quarantine").ShouldBeTrue();
        result.Items.Any(a => a.Name == "Cow No Tag").ShouldBeFalse();
    }

    private async Task<(Farm farm, Lot lot, User user)> SetupFarmWithLotAsync(
        string role,
        string suffix
    )
    {
        var user = new User
        {
            Name = $"User {suffix}",
            Email = $"user_{suffix}@animals.com",
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
