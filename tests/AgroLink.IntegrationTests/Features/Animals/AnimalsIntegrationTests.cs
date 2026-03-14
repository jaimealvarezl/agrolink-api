using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
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
}
