using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Api.DTOs.Farms;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Farms;

public class FarmsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Test]
    public async Task Create_WithValidData_ShouldReturnCreated()
    {
        // Arrange
        var user = new User
        {
            Name = "Owner",
            Email = "owner@farm.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        var request = new CreateFarmRequest
        {
            Name = "Integration Farm",
            Location = "Test Location",
            CUE = "123456",
        };

        // Act
        var response = await Client.PostAsJsonAsync("/api/Farms", request);

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        var created = await response.Content.ReadFromJsonAsync<FarmDto>(JsonOptions);
        created.ShouldNotBeNull();
        created.Name.ShouldBe(request.Name);

        // Verify in DB
        var dbFarm = await DbContext.Farms.FindAsync(created.Id);
        dbFarm.ShouldNotBeNull();
        dbFarm.Name.ShouldBe(request.Name);
    }

    [Test]
    public async Task GetAll_ShouldReturnUserFarms()
    {
        // Arrange
        var user = new User
        {
            Name = "Farm User",
            Email = "user@farm.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);
        await DbContext.SaveChangesAsync();

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm1 = new Farm { Name = "Farm 1", OwnerId = owner.Id };
        var farm2 = new Farm { Name = "Farm 2", OwnerId = owner.Id };
        DbContext.Farms.AddRange(farm1, farm2);
        await DbContext.SaveChangesAsync();

        // Add user as member to farm1 only
        DbContext.FarmMembers.Add(
            new FarmMember
            {
                FarmId = farm1.Id,
                UserId = user.Id,
                Role = FarmMemberRoles.Owner,
            }
        );
        await DbContext.SaveChangesAsync();

        Authenticate(user);

        // Act
        var response = await Client.GetAsync("/api/Farms");

        // Assert
        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var farms = await response.Content.ReadFromJsonAsync<IEnumerable<FarmDto>>(JsonOptions);
        farms.ShouldNotBeNull();
        farms.Count().ShouldBe(1);
        farms.First().Name.ShouldBe("Farm 1");
    }
}
