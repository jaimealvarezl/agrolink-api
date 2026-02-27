using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Api.Controllers;
using AgroLink.Application.Features.Lots.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Lots;

public class LotsIntegrationTests : IntegrationTestBase
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
            Email = "admin@lot.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);

        var paddock = new Paddock { Name = "P1", FarmId = 0 }; // Will set after farm save

        // Relational save
        farm.Paddocks.Add(paddock);
        DbContext.Farms.Add(farm);
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

        var request = new CreateLotRequest
        {
            Name = "Lot 1",
            PaddockId = paddock.Id,
            Status = "ACTIVE",
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/lots", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<LotDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.Name.ShouldBe(request.Name);
    }

    [Test]
    public async Task GetByPaddock_ShouldReturnLots()
    {
        // Arrange
        var user = new User
        {
            Name = "Viewer",
            Email = "viewer@lot.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
        var paddock = new Paddock { Name = "P1", FarmId = 0 };
        farm.Paddocks.Add(paddock);
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Viewer,
            }
        );

        var lot1 = new Lot
        {
            Name = "L1",
            PaddockId = paddock.Id,
            Status = "Active",
        };
        var lot2 = new Lot
        {
            Name = "L2",
            PaddockId = paddock.Id,
            Status = "Active",
        };
        DbContext.Lots.AddRange(lot1, lot2);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/lots/paddock/{paddock.Id}");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var lots = await response.Content.ReadFromJsonAsync<IEnumerable<LotDto>>(JsonOptions);
        lots.ShouldNotBeNull();
        lots.Count().ShouldBe(2);
    }
}
