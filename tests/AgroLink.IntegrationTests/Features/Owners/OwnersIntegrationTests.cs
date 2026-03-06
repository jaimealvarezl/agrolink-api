using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Api.DTOs.Owners;
using AgroLink.Application.Features.Owners.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Owners;

public class OwnersIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Test]
    public async Task Create_AsAdmin_ShouldReturnCreated()
    {
        // Arrange
        var user = new User
        {
            Name = "Admin",
            Email = "admin@owners.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm" };
        var mainOwner = new Owner { Name = "Main Owner", Phone = "123" };
        DbContext.Owners.Add(mainOwner);
        await DbContext.SaveChangesAsync();

        farm.OwnerId = mainOwner.Id;
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        mainOwner.FarmId = farm.Id;
        await DbContext.SaveChangesAsync();

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

        var request = new CreateOwnerRequest
        {
            Name = "New Partner",
            Phone = "555123",
            Email = "partner@test.com",
            UserId = user.Id,
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/owners", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<OwnerDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.Name.ShouldBe(request.Name);
        created.Phone.ShouldBe(request.Phone);
        created.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task GetOwners_AsViewer_ShouldReturnForbidden()
    {
        // Arrange
        var user = new User
        {
            Name = "Viewer",
            Email = "viewer@owners.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm" };
        var mainOwner = new Owner { Name = "Main Owner", Phone = "123" };
        DbContext.Owners.Add(mainOwner);
        await DbContext.SaveChangesAsync();

        farm.OwnerId = mainOwner.Id;
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        mainOwner.FarmId = farm.Id;
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Viewer, // Only Admin/Owner can GET owners
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/owners");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Forbidden);
    }

    [Test]
    public async Task GetOwners_AsAdmin_ShouldReturnOnlyActiveOwners()
    {
        // Arrange
        var user = new User
        {
            Name = "Admin",
            Email = "admin2@owners.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm" };
        var mainOwner = new Owner
        {
            Name = "Main Owner",
            Phone = "123",
            IsActive = true,
        };
        DbContext.Owners.Add(mainOwner);
        await DbContext.SaveChangesAsync();

        farm.OwnerId = mainOwner.Id;
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        mainOwner.FarmId = farm.Id;
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Admin,
            }
        );

        var inactiveOwner = new Owner
        {
            Name = "Deleted Partner",
            FarmId = farm.Id,
            IsActive = false,
        };
        var activePartner = new Owner
        {
            Name = "Active Partner",
            FarmId = farm.Id,
            IsActive = true,
        };
        DbContext.Owners.AddRange(inactiveOwner, activePartner);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/owners");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var owners = await response.Content.ReadFromJsonAsync<IEnumerable<OwnerDto>>(JsonOptions);
        owners.ShouldNotBeNull();
        owners.Count().ShouldBe(2); // mainOwner + activePartner
        owners.ShouldContain(o => o.Name == "Active Partner");
        owners.ShouldNotContain(o => o.Name == "Deleted Partner");
    }

    [Test]
    public async Task Delete_AsOwner_ShouldSoftDelete()
    {
        // Arrange
        var user = new User
        {
            Name = "Owner User",
            Email = "owner@owners.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm" };
        var mainOwner = new Owner { Name = "Main Owner", Phone = "123" };
        DbContext.Owners.Add(mainOwner);
        await DbContext.SaveChangesAsync();

        farm.OwnerId = mainOwner.Id;
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        mainOwner.FarmId = farm.Id;
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Owner, // Requires Owner
            }
        );

        var partnerToDelete = new Owner
        {
            Name = "Partner To Delete",
            FarmId = farm.Id,
            IsActive = true,
        };
        DbContext.Owners.Add(partnerToDelete);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.DeleteAsync(
            $"/api/farms/{farm.Id}/owners/{partnerToDelete.Id}"
        );

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);

        // Verify it was soft-deleted in DB
        DbContext.ChangeTracker.Clear();
        var dbOwner = await DbContext
            .Owners.IgnoreQueryFilters()
            .FirstOrDefaultAsync(o => o.Id == partnerToDelete.Id);
        dbOwner.ShouldNotBeNull();
        dbOwner.IsActive.ShouldBeFalse();
    }

    [Test]
    public async Task Delete_MainOwner_ShouldReturnBadRequest()
    {
        // Arrange
        var user = new User
        {
            Name = "Main Owner User",
            Email = "mainowner@owners.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm" };
        var mainOwner = new Owner
        {
            Name = "Main Owner",
            Phone = "123",
            IsActive = true,
        };
        DbContext.Owners.Add(mainOwner);
        await DbContext.SaveChangesAsync();

        farm.OwnerId = mainOwner.Id;
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        mainOwner.FarmId = farm.Id;
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Owner,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.DeleteAsync($"/api/farms/{farm.Id}/owners/{mainOwner.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Create_WithExistingDeletedOwner_ShouldRestoreOwner()
    {
        // Arrange
        var user = new User
        {
            Name = "Admin User",
            Email = "adminrestore@owners.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm" };
        var mainOwner = new Owner { Name = "Main Owner", Phone = "123", IsActive = true };
        DbContext.Owners.Add(mainOwner);
        await DbContext.SaveChangesAsync();

        farm.OwnerId = mainOwner.Id;
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        mainOwner.FarmId = farm.Id;
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Admin,
            }
        );
        
        var deletedOwner = new Owner { Name = "Restore Me", FarmId = farm.Id, IsActive = false, Phone = "Old Phone" };
        DbContext.Owners.Add(deletedOwner);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var request = new CreateOwnerRequest
        {
            Name = "Restore Me", // Must match exactly
            Phone = "New Phone",
            Email = "restored@test.com",
            UserId = user.Id
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/owners", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        
        DbContext.ChangeTracker.Clear();
        var restoredOwner = await DbContext.Owners.FindAsync(deletedOwner.Id);
        
        restoredOwner.ShouldNotBeNull();
        restoredOwner.IsActive.ShouldBeTrue();
        restoredOwner.Phone.ShouldBe("New Phone");
        restoredOwner.Email.ShouldBe("restored@test.com");
        restoredOwner.UserId.ShouldBe(user.Id);
    }
}
