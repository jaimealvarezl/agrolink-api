using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Api.DTOs.OwnerBrands;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.OwnerBrands;

public class OwnerBrandsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private async Task<(Farm farm, Owner owner, User adminUser)> SetupFarmWithAdminAsync(
        string emailSuffix
    )
    {
        var user = new User
        {
            Name = "Admin",
            Email = $"admin_{emailSuffix}@brands.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var owner = new Owner
        {
            Name = "Main Owner",
            Phone = "123",
            IsActive = true,
        };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
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
        await DbContext.SaveChangesAsync();

        Authenticate(user);
        return (farm, owner, user);
    }

    private async Task<OwnerBrand> CreateOwnerBrandAsync(
        int ownerId,
        string description = "Test brand"
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

    // GET tests

    [Test]
    public async Task GetBrands_AsAdmin_ReturnsActiveBrands()
    {
        // Arrange
        var (farm, owner, _) = await SetupFarmWithAdminAsync("get1");
        await CreateOwnerBrandAsync(owner.Id, "Brand One");
        await CreateOwnerBrandAsync(owner.Id, "Brand Two");

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/owners/{owner.Id}/brands");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var brands = await response.Content.ReadFromJsonAsync<IEnumerable<OwnerBrandDto>>(
            JsonOptions
        );
        brands.ShouldNotBeNull();
        brands.Count().ShouldBe(2);
    }

    [Test]
    public async Task GetBrands_AsViewer_ReturnsForbidden()
    {
        // Arrange
        var user = new User
        {
            Name = "Viewer",
            Email = "viewer@brands.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

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
                Role = FarmMemberRoles.Viewer,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/owners/{owner.Id}/brands");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetBrands_DoesNotReturnInactiveBrands()
    {
        // Arrange
        var (farm, owner, _) = await SetupFarmWithAdminAsync("get2");
        await CreateOwnerBrandAsync(owner.Id, "Active brand");

        DbContext.OwnerBrands.Add(
            new OwnerBrand
            {
                OwnerId = owner.Id,
                Description = "Inactive brand",
                IsActive = false,
            }
        );
        await DbContext.SaveChangesAsync();

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/owners/{owner.Id}/brands");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var brands = await response.Content.ReadFromJsonAsync<IEnumerable<OwnerBrandDto>>(
            JsonOptions
        );
        brands.ShouldNotBeNull();
        brands.Count().ShouldBe(1);
        brands.First().Description.ShouldBe("Active brand");
    }

    // POST tests

    [Test]
    public async Task Create_AsAdmin_ReturnsCreated()
    {
        // Arrange
        var (farm, owner, _) = await SetupFarmWithAdminAsync("post1");

        var request = new CreateOwnerBrandRequest { Description = "Tres rayas / Letra J" };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/owners/{owner.Id}/brands",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OwnerBrandDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.Description.ShouldBe("Tres rayas / Letra J");
        created.PhotoUrl.ShouldBeNull();
        created.OwnerId.ShouldBe(owner.Id);
        created.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task Create_AsForeman_ReturnsForbidden()
    {
        // Arrange
        var user = new User
        {
            Name = "Foreman",
            Email = "foreman@brands.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

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
                Role = FarmMemberRoles.Editor,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var request = new CreateOwnerBrandRequest { Description = "Brand" };

        // Act
        var response = await Client.PostAsJsonAsync(
            $"/api/farms/{farm.Id}/owners/{owner.Id}/brands",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    // PUT tests

    [Test]
    public async Task Update_AsAdmin_ReturnsUpdatedBrand()
    {
        // Arrange
        var (farm, owner, _) = await SetupFarmWithAdminAsync("put1");
        var brand = await CreateOwnerBrandAsync(owner.Id, "Old description");

        var request = new UpdateOwnerBrandRequest { Description = "Updated description" };

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/farms/{farm.Id}/owners/{owner.Id}/brands/{brand.Id}",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var updated = await response.Content.ReadFromJsonAsync<OwnerBrandDto>(JsonOptions);
        updated.ShouldNotBeNull();
        updated.Description.ShouldBe("Updated description");
        updated.UpdatedAt.ShouldNotBeNull();

        DbContext.ChangeTracker.Clear();
        var dbBrand = await DbContext
            .OwnerBrands.IgnoreQueryFilters()
            .FirstAsync(b => b.Id == brand.Id);
        dbBrand.Description.ShouldBe("Updated description");
    }

    [Test]
    public async Task Update_BrandNotFound_ReturnsNotFound()
    {
        // Arrange
        var (farm, owner, _) = await SetupFarmWithAdminAsync("put2");

        var request = new UpdateOwnerBrandRequest { Description = "Brand" };

        // Act
        var response = await Client.PutAsJsonAsync(
            $"/api/farms/{farm.Id}/owners/{owner.Id}/brands/9999",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    // DELETE tests

    [Test]
    public async Task Delete_AsAdmin_SoftDeletesBrand()
    {
        // Arrange
        var (farm, owner, _) = await SetupFarmWithAdminAsync("del1");
        var brand = await CreateOwnerBrandAsync(owner.Id);

        // Act
        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/owners/{owner.Id}/brands/{brand.Id}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        DbContext.ChangeTracker.Clear();
        var dbBrand = await DbContext
            .OwnerBrands.IgnoreQueryFilters()
            .FirstAsync(b => b.Id == brand.Id);
        dbBrand.IsActive.ShouldBeFalse();
        dbBrand.UpdatedAt.ShouldNotBeNull();
    }

    [Test]
    public async Task Delete_BrandNotFound_ReturnsNotFound()
    {
        // Arrange
        var (farm, owner, _) = await SetupFarmWithAdminAsync("del2");

        // Act
        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/owners/{owner.Id}/brands/9999"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }
}
