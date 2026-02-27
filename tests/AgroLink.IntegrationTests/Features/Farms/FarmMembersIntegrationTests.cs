using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Api.DTOs.Farms;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Farms;

public class FarmMembersIntegrationTests : IntegrationTestBase
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
    public async Task GetMembers_AsOwner_ShouldReturnList()
    {
        // Arrange
        var (user, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);
        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/Farms/{farm.Id}/members");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var members = await response.Content.ReadFromJsonAsync<IEnumerable<FarmMemberDto>>(
            JsonOptions
        );
        members.ShouldNotBeNull();
        members.ShouldContain(m => m.Email == user.Email && m.Role == FarmMemberRoles.Owner);
    }

    [Test]
    public async Task AddMember_AsOwner_ShouldSucceed()
    {
        // Arrange
        var (owner, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);
        var newUser = new User
        {
            Name = "New User",
            Email = "new@test.com",
            PasswordHash = "hash",
        };
        DbContext.Users.Add(newUser);
        await DbContext.SaveChangesAsync();

        Authenticate(owner);

        var request = new AddMemberRequest { Email = newUser.Email, Role = FarmMemberRoles.Editor };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Farms/{farm.Id}/members", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FarmMemberDto>(JsonOptions);
        result.ShouldNotBeNull();
        result.Email.ShouldBe(newUser.Email);
        result.Role.ShouldBe(FarmMemberRoles.Editor);

        // Verify in DB
        var dbMember = DbContext.FarmMembers.FirstOrDefault(m =>
            m.FarmId == farm.Id && m.UserId == newUser.Id
        );
        dbMember.ShouldNotBeNull();
        dbMember.Role.ShouldBe(FarmMemberRoles.Editor);
    }

    [Test]
    public async Task AddMember_UserNotFound_ShouldReturnBadRequest()
    {
        // Arrange
        var (owner, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);
        Authenticate(owner);

        var request = new AddMemberRequest
        {
            Email = "nonexistent@test.com",
            Role = FarmMemberRoles.Editor,
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/Farms/{farm.Id}/members", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("User not found");
    }

    [Test]
    public async Task UpdateMemberRole_AsOwner_ShouldSucceed()
    {
        // Arrange
        var (owner, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);
        var memberUser = new User
        {
            Name = "Member",
            Email = "member@test.com",
            PasswordHash = "hash",
        };
        DbContext.Users.Add(memberUser);
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = memberUser.Id,
                Role = FarmMemberRoles.Viewer,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(owner);

        var request = new UpdateMemberRoleRequest { Role = FarmMemberRoles.Admin };

        // Act
        var response = await Client.PatchAsJsonAsync(
            $"/api/Farms/{farm.Id}/members/{memberUser.Id}",
            request
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var result = await response.Content.ReadFromJsonAsync<FarmMemberDto>(JsonOptions);
        result.ShouldNotBeNull();
        result.Role.ShouldBe(FarmMemberRoles.Admin);
    }

    [Test]
    public async Task RemoveMember_AsOwner_ShouldSucceed()
    {
        // Arrange
        var (owner, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);
        var memberUser = new User
        {
            Name = "Member",
            Email = "member@test.com",
            PasswordHash = "hash",
        };
        DbContext.Users.Add(memberUser);
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = memberUser.Id,
                Role = FarmMemberRoles.Viewer,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(owner);

        // Act
        var response = await Client.DeleteAsync($"/api/Farms/{farm.Id}/members/{memberUser.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify in DB
        var dbMember = DbContext.FarmMembers.FirstOrDefault(m =>
            m.FarmId == farm.Id && m.UserId == memberUser.Id
        );
        dbMember.ShouldBeNull();
    }

    [Test]
    public async Task RemoveMember_LastOwner_ShouldReturnBadRequest()
    {
        // Arrange
        var (owner, farm) = await SetupFarmWithUser("owner@test.com", FarmMemberRoles.Owner);
        Authenticate(owner);

        // Act
        var response = await Client.DeleteAsync($"/api/Farms/{farm.Id}/members/{owner.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        var body = await response.Content.ReadAsStringAsync();
        body.ShouldContain("Cannot remove the last owner");
    }
}
