using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Farms;

public class FarmPermissionsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    private async Task<(User User, Farm Farm)> SetupFarmWithUser(string email, string role)
    {
        var user = new User
        {
            Name = "Test User",
            Email = email,
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var owner = new Owner { Name = "Farm Owner", Phone = "123" };
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

        return (user, farm);
    }

    [Test]
    public async Task GetPermissions_AsOwner_ReturnsAllTrue()
    {
        var (user, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var permissions = await response.Content.ReadFromJsonAsync<FarmPermissionsDto>(JsonOptions);
        permissions.ShouldNotBeNull();
        permissions.CanCreateAnimal.ShouldBeTrue();
        permissions.CanDeleteAnimal.ShouldBeTrue();
        permissions.CanDeleteFarm.ShouldBeTrue();
        permissions.CanManageTeam.ShouldBeTrue();
        permissions.CanUpdateFarmMetadata.ShouldBeTrue();
        permissions.CanManageLocations.ShouldBeTrue();
        permissions.CanViewFinancials.ShouldBeTrue();
        permissions.CanViewChecklists.ShouldBeTrue();
    }

    [Test]
    public async Task GetPermissions_AsAdmin_ReturnsAdminPermissions()
    {
        var (user, farm) = await SetupFarmWithUser("admin@test.com", FarmMemberRoles.Admin);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var permissions = await response.Content.ReadFromJsonAsync<FarmPermissionsDto>(JsonOptions);
        permissions.ShouldNotBeNull();
        permissions.CanDeleteAnimal.ShouldBeTrue();
        permissions.CanManageLocations.ShouldBeTrue();
        permissions.CanViewFinancials.ShouldBeTrue();
        permissions.CanDeleteFarm.ShouldBeFalse();
        permissions.CanManageTeam.ShouldBeFalse();
        permissions.CanUpdateFarmMetadata.ShouldBeFalse();
    }

    [Test]
    public async Task GetPermissions_AsEditor_ReturnsEditorPermissions()
    {
        var (user, farm) = await SetupFarmWithUser("editor@test.com", FarmMemberRoles.Editor);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var permissions = await response.Content.ReadFromJsonAsync<FarmPermissionsDto>(JsonOptions);
        permissions.ShouldNotBeNull();
        permissions.CanCreateAnimal.ShouldBeTrue();
        permissions.CanLogOperations.ShouldBeTrue();
        permissions.CanViewChecklists.ShouldBeTrue();
        permissions.CanDeleteAnimal.ShouldBeFalse();
        permissions.CanManageLocations.ShouldBeFalse();
        permissions.CanViewFinancials.ShouldBeFalse();
        permissions.CanDeleteFarm.ShouldBeFalse();
    }

    [Test]
    public async Task GetPermissions_AsViewer_ReturnsViewerPermissions()
    {
        var (user, farm) = await SetupFarmWithUser("viewer@test.com", FarmMemberRoles.Viewer);
        Authenticate(user);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var permissions = await response.Content.ReadFromJsonAsync<FarmPermissionsDto>(JsonOptions);
        permissions.ShouldNotBeNull();
        permissions.CanViewChecklists.ShouldBeTrue();
        permissions.CanCreateAnimal.ShouldBeFalse();
        permissions.CanDeleteAnimal.ShouldBeFalse();
        permissions.CanViewFinancials.ShouldBeFalse();
        permissions.CanDeleteFarm.ShouldBeFalse();
        permissions.CanManageTeam.ShouldBeFalse();
    }

    [Test]
    public async Task GetPermissions_Unauthenticated_Returns401()
    {
        var (_, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [Test]
    public async Task GetPermissions_NonMember_Returns403()
    {
        var (_, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);

        var nonMember = new User
        {
            Name = "Stranger",
            Email = "stranger@test.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(nonMember);
        await DbContext.SaveChangesAsync();

        Authenticate(nonMember);

        var response = await Client.GetAsync($"/api/farms/{farm.Id}/permissions");

        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }
}
