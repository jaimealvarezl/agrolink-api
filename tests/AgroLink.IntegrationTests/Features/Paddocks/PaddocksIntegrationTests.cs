using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Api.DTOs.Paddocks;
using AgroLink.Application.Features.Paddocks.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Paddocks;

public class PaddocksIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() }
    };

    [Test]
    public async Task Create_AsAdmin_ShouldReturnCreated()
    {
        // Arrange
        var user = new User { Name = "Admin", Email = "admin@farm.com", PasswordHash = "hash", Role = "USER" };
        DbContext.Users.Add(user);
        
        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(new FarmMember { FarmId = farm.Id, UserId = user.Id, Role = FarmMemberRoles.Admin });
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var request = new CreatePaddockRequest
        {
            Name = "Paddock 1",
            FarmId = farm.Id,
            Area = 10.5m,
            AreaType = AreaTypes.Hectare
        };

        // Act
        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/paddocks", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<PaddockDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.Name.ShouldBe(request.Name);
    }

    [Test]
    public async Task GetAll_ShouldReturnFarmPaddocks()
    {
        // Arrange
        var user = new User { Name = "Viewer", Email = "viewer@farm.com", PasswordHash = "hash", Role = "USER" };
        DbContext.Users.Add(user);
        
        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        DbContext.FarmMembers.Add(new FarmMember { FarmId = farm.Id, UserId = user.Id, Role = FarmMemberRoles.Viewer });
        
        var p1 = new Paddock { Name = "P1", FarmId = farm.Id };
        var p2 = new Paddock { Name = "P2", FarmId = farm.Id };
        DbContext.Paddocks.AddRange(p1, p2);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync($"/api/farms/{farm.Id}/paddocks");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var paddocks = await response.Content.ReadFromJsonAsync<IEnumerable<PaddockDto>>(JsonOptions);
        paddocks.ShouldNotBeNull();
        paddocks.Count().ShouldBe(2);
    }
}
