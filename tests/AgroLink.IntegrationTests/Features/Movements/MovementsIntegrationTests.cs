using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AgroLink.Application.Features.Movements.DTOs;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using Shouldly;

namespace AgroLink.IntegrationTests.Features.Movements;

public class MovementsIntegrationTests : IntegrationTestBase
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    [Test]
    public async Task CreateBatchMovement_AsAdmin_ShouldReturnOkAndCreateMovements()
    {
        var user = new User
        {
            Name = "Admin",
            Email = "admin@movements.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        var paddock = new Paddock { Name = "P1", FarmId = farm.Id };
        DbContext.Paddocks.Add(paddock);
        await DbContext.SaveChangesAsync();

        var sourceLot = new Lot
        {
            Name = "Source Lot",
            PaddockId = paddock.Id,
            Status = "ACTIVE",
        };
        var destinationLot = new Lot
        {
            Name = "Dest Lot",
            PaddockId = paddock.Id,
            Status = "ACTIVE",
        };
        DbContext.Lots.AddRange(sourceLot, destinationLot);
        await DbContext.SaveChangesAsync();

        var animal1 = new Animal
        {
            TagVisual = "A1",
            LotId = sourceLot.Id,
            BirthDate = DateTime.UtcNow,
        };
        var animal2 = new Animal
        {
            TagVisual = "A2",
            LotId = sourceLot.Id,
            BirthDate = DateTime.UtcNow,
        };
        DbContext.Animals.AddRange(animal1, animal2);
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

        var request = new CreateMovementDto
        {
            AnimalIds = new List<int> { animal1.Id, animal2.Id },
            ToLotId = destinationLot.Id,
            At = DateTime.UtcNow,
            Reason = "Batch Move Test",
        };

        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/movements", request);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var createdMovements = await response.Content.ReadFromJsonAsync<IEnumerable<MovementDto>>(
            JsonOptions
        );

        createdMovements.ShouldNotBeNull();
        var movementsList = createdMovements.ToList();
        movementsList.Count.ShouldBe(2);

        var m1 = movementsList.FirstOrDefault(m => m.AnimalId == animal1.Id);
        m1.ShouldNotBeNull();
        m1.FromLotId.ShouldBe(sourceLot.Id);
        m1.ToLotId.ShouldBe(destinationLot.Id);

        var m2 = movementsList.FirstOrDefault(m => m.AnimalId == animal2.Id);
        m2.ShouldNotBeNull();
        m2.FromLotId.ShouldBe(sourceLot.Id);
        m2.ToLotId.ShouldBe(destinationLot.Id);

        // Verify DB State directly
        DbContext.ChangeTracker.Clear(); // Ensure fresh DB read
        var updatedAnimal1 = await DbContext.Animals.FindAsync(animal1.Id);
        updatedAnimal1!.LotId.ShouldBe(destinationLot.Id);

        var updatedAnimal2 = await DbContext.Animals.FindAsync(animal2.Id);
        updatedAnimal2!.LotId.ShouldBe(destinationLot.Id);
    }

    [Test]
    public async Task CreateBatchMovement_WithInvalidToLot_ShouldReturnBadRequest()
    {
        var user = new User
        {
            Name = "Admin2",
            Email = "admin2@movements.com",
            PasswordHash = "hash",
            Role = "USER",
        };
        DbContext.Users.Add(user);

        var owner = new Owner { Name = "Owner", Phone = "123" };
        DbContext.Owners.Add(owner);
        await DbContext.SaveChangesAsync();

        var farm = new Farm { Name = "Test Farm 2", OwnerId = owner.Id };
        DbContext.Farms.Add(farm);
        await DbContext.SaveChangesAsync();

        var paddock = new Paddock { Name = "P1", FarmId = farm.Id };
        DbContext.Paddocks.Add(paddock);
        await DbContext.SaveChangesAsync();

        var sourceLot = new Lot
        {
            Name = "Source Lot",
            PaddockId = paddock.Id,
            Status = "ACTIVE",
        };
        DbContext.Lots.Add(sourceLot);
        await DbContext.SaveChangesAsync();

        var animal1 = new Animal
        {
            TagVisual = "A1",
            LotId = sourceLot.Id,
            BirthDate = DateTime.UtcNow,
        };
        DbContext.Animals.Add(animal1);
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

        var request = new CreateMovementDto
        {
            AnimalIds = new List<int> { animal1.Id },
            ToLotId = 9999, // Invalid Lot
            At = DateTime.UtcNow,
            Reason = "Bad Move",
        };

        var response = await Client.PostAsJsonAsync($"/api/farms/{farm.Id}/movements", request);

        // GlobalExceptionFilter currently handles ArgumentException as BadRequest
        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }
}
