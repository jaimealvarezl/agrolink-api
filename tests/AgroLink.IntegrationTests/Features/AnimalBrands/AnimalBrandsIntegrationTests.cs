using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Api.DTOs.AnimalBrands;
using AgroLink.Application.Features.AnimalBrands.DTOs;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.AnimalBrands;

public class AnimalBrandsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private async Task<(Farm farm, Owner owner, Animal animal, User adminUser)> SetupAsync(
        string emailSuffix
    )
    {
        var user = new User
        {
            Name = "Admin",
            Email = $"admin_{emailSuffix}@ab.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner
        {
            Name = "Owner",
            Phone = "123",
            IsActive = true,
        };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Farm", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        owner.FarmId = farm.Id;
        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Admin,
            }
        );

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
            Name = "Cow",
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

        Authenticate(user);
        return (farm, owner, animal, user);
    }

    private async Task<OwnerBrand> CreateOwnerBrandAsync(
        int ownerId,
        string description = "Brand X"
    )
    {
        var brand = new OwnerBrand
        {
            OwnerId = ownerId,
            Description = description,
            IsActive = true,
        };
        DbContext.OwnerBrands.Add(brand);
        await DbContext.SaveChangesAsync();
        return brand;
    }

    private async Task<AnimalBrand> CreateAnimalBrandAsync(int animalId, int ownerBrandId)
    {
        var ab = new AnimalBrand
        {
            AnimalId = animalId,
            OwnerBrandId = ownerBrandId,
            AppliedAt = DateTime.UtcNow.Date,
        };
        DbContext.AnimalBrands.Add(ab);
        await DbContext.SaveChangesAsync();
        return ab;
    }

    // GET /brands

    [Test]
    public async Task GetBrands_AsViewer_ReturnsAnimalBrands()
    {
        // Arrange
        var (farm, owner, animal, _) = await SetupAsync("get1");
        var ownerBrand = await CreateOwnerBrandAsync(owner.Id);
        await CreateAnimalBrandAsync(animal.Id, ownerBrand.Id);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/{animal.Id}/brands");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var brands = await response.Content.ReadFromJsonAsync<IEnumerable<AnimalBrandDto>>(
            JsonOptions
        );
        brands.ShouldNotBeNull();
        brands.Count().ShouldBe(1);
        brands.First().OwnerBrandId.ShouldBe(ownerBrand.Id);
    }

    [Test]
    public async Task GetBrands_AnimalNotInFarm_ReturnsNotFound()
    {
        // Arrange
        var (farm, _, _, _) = await SetupAsync("get2");

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/9999/brands");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task GetBrands_Unauthenticated_ReturnsUnauthorized()
    {
        // Arrange
        var (farm, _, animal, _) = await SetupAsync("get3");
        Client.DefaultRequestHeaders.Authorization = null;

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/animals/{animal.Id}/brands");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    // POST /brands

    [Test]
    public async Task AddBrand_AsEditor_ReturnsCreated()
    {
        // Arrange
        var (farm, owner, animal, _) = await SetupAsync("post1");
        var ownerBrand = await CreateOwnerBrandAsync(owner.Id, "Tres rayas");

        var request = new AddAnimalBrandRequest
        {
            OwnerBrandId = ownerBrand.Id,
            AppliedAt = DateTime.UtcNow.Date,
            Notes = "Applied at birth",
        };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brands",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<AnimalBrandDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.AnimalId.ShouldBe(animal.Id);
        created.OwnerBrandId.ShouldBe(ownerBrand.Id);
        created.Notes.ShouldBe("Applied at birth");
        created.OwnerBrand.Description.ShouldBe("Tres rayas");
    }

    [Test]
    public async Task AddBrand_OwnerBrandNotInFarm_ReturnsNotFound()
    {
        // Arrange
        var (farm, _, animal, _) = await SetupAsync("post2");

        // Create an owner from another farm
        var otherOwner = new Owner
        {
            Name = "Other Owner",
            Phone = "999",
            IsActive = true,
        };
        DbContext.Owners.Add(otherOwner);
        await DbContext.SaveChangesAsync();

        var otherFarm = new Farm { Name = "Other Farm", OwnerId = otherOwner.Id };
        DbContext.Farms.Add(otherFarm);
        await DbContext.SaveChangesAsync();
        otherOwner.FarmId = otherFarm.Id;
        await DbContext.SaveChangesAsync();

        var foreignBrand = new OwnerBrand
        {
            OwnerId = otherOwner.Id,
            Description = "Foreign brand",
            IsActive = true,
        };
        DbContext.OwnerBrands.Add(foreignBrand);
        await DbContext.SaveChangesAsync();

        var request = new AddAnimalBrandRequest { OwnerBrandId = foreignBrand.Id };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brands",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task AddBrand_AsViewer_ReturnsForbidden()
    {
        // Arrange
        var (farm, owner, animal, adminUser) = await SetupAsync("post3");
        var ownerBrand = await CreateOwnerBrandAsync(owner.Id);

        var viewerUser = new User
        {
            Name = "Viewer",
            Email = "viewer_post3@ab.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(viewerUser);
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = viewerUser.Id,
                Role = FarmMemberRoles.Viewer,
            }
        );
        await DbContext.SaveChangesAsync();
        Authenticate(viewerUser);

        var request = new AddAnimalBrandRequest { OwnerBrandId = ownerBrand.Id };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brands",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // DELETE /brands/{animalBrandId}

    [Test]
    public async Task RemoveBrand_AsEditor_ReturnsNoContent()
    {
        // Arrange
        var (farm, owner, animal, _) = await SetupAsync("del1");
        var ownerBrand = await CreateOwnerBrandAsync(owner.Id);
        var animalBrand = await CreateAnimalBrandAsync(animal.Id, ownerBrand.Id);

        // Act
        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brands/{animalBrand.Id}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        DbContext.ChangeTracker.Clear();
        var deleted = await DbContext
            .AnimalBrands.IgnoreQueryFilters()
            .FirstOrDefaultAsync(ab => ab.Id == animalBrand.Id);
        deleted.ShouldBeNull();
    }

    [Test]
    public async Task RemoveBrand_NotFound_ReturnsNotFound()
    {
        // Arrange
        var (farm, _, animal, _) = await SetupAsync("del2");

        // Act
        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brands/9999"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task RemoveBrand_BrandBelongsToOtherAnimal_ReturnsNotFound()
    {
        // Arrange
        var (farm, owner, animal, _) = await SetupAsync("del3");

        var paddock = await DbContext.Paddocks.FirstAsync(p => p.FarmId == farm.Id);
        var lot = await DbContext.Lots.FirstAsync(l => l.PaddockId == paddock.Id);

        var otherAnimal = new Animal
        {
            Name = "Bull",
            Sex = Sex.Male,
            LotId = lot.Id,
            BirthDate = DateTime.UtcNow.AddYears(-3),
            LifeStatus = LifeStatus.Active,
            ProductionStatus = ProductionStatus.Calf,
            HealthStatus = HealthStatus.Healthy,
            ReproductiveStatus = ReproductiveStatus.NotApplicable,
        };
        DbContext.Animals.Add(otherAnimal);
        await DbContext.SaveChangesAsync();

        var ownerBrand = await CreateOwnerBrandAsync(owner.Id);
        var brandOnOtherAnimal = await CreateAnimalBrandAsync(otherAnimal.Id, ownerBrand.Id);

        // Act — try to delete other animal's brand using first animal's route
        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brands/{brandOnOtherAnimal.Id}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // GET /brand-suggestions

    [Test]
    public async Task GetSuggestions_ReturnsActiveOwnerBrandsFromCurrentOwners()
    {
        // Arrange
        var (farm, owner, animal, _) = await SetupAsync("sug1");

        // Link animal to owner
        DbContext.AnimalOwners.Add(
            new AnimalOwner
            {
                AnimalId = animal.Id,
                OwnerId = owner.Id,
                SharePercent = 100,
            }
        );
        await DbContext.SaveChangesAsync();

        var activeBrand = await CreateOwnerBrandAsync(owner.Id, "Active brand");
        var inactiveBrand = new OwnerBrand
        {
            OwnerId = owner.Id,
            Description = "Inactive brand",
            IsActive = false,
        };
        DbContext.OwnerBrands.Add(inactiveBrand);
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brand-suggestions"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var suggestions = await response.Content.ReadFromJsonAsync<IEnumerable<OwnerBrandDto>>(
            JsonOptions
        );
        suggestions.ShouldNotBeNull();
        suggestions.Count().ShouldBe(1);
        suggestions.First().Description.ShouldBe("Active brand");
    }

    [Test]
    public async Task GetSuggestions_NoOwners_ReturnsEmptyList()
    {
        // Arrange
        var (farm, _, animal, _) = await SetupAsync("sug2");

        // Act
        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brand-suggestions"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var suggestions = await response.Content.ReadFromJsonAsync<IEnumerable<OwnerBrandDto>>(
            JsonOptions
        );
        suggestions.ShouldNotBeNull();
        suggestions.ShouldBeEmpty();
    }

    [Test]
    public async Task GetSuggestions_MultipleOwners_ReturnsBrandsFromAll()
    {
        // Arrange
        var (farm, owner1, animal, _) = await SetupAsync("sug3");

        var owner2 = new Owner
        {
            Name = "Owner 2",
            Phone = "456",
            IsActive = true,
            FarmId = farm.Id,
        };
        DbContext.Owners.Add(owner2);
        await DbContext.SaveChangesAsync();

        DbContext.AnimalOwners.Add(
            new AnimalOwner
            {
                AnimalId = animal.Id,
                OwnerId = owner1.Id,
                SharePercent = 50,
            }
        );
        DbContext.AnimalOwners.Add(
            new AnimalOwner
            {
                AnimalId = animal.Id,
                OwnerId = owner2.Id,
                SharePercent = 50,
            }
        );
        await DbContext.SaveChangesAsync();

        await CreateOwnerBrandAsync(owner1.Id, "Brand from owner 1");
        await CreateOwnerBrandAsync(owner2.Id, "Brand from owner 2");

        // Act
        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/{animal.Id}/brand-suggestions"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var suggestions = await response.Content.ReadFromJsonAsync<IEnumerable<OwnerBrandDto>>(
            JsonOptions
        );
        suggestions.ShouldNotBeNull();
        suggestions.Count().ShouldBe(2);
    }

    [Test]
    public async Task GetSuggestions_AnimalNotInFarm_ReturnsNotFound()
    {
        // Arrange
        var (farm, _, _, _) = await SetupAsync("sug4");

        // Act
        var response = await Client.GetAsync(
            $"/api/farms/{farm.Id}/animals/9999/brand-suggestions"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
