using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Application.Services;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Repositories;
using AgroLink.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

// Added
// Added for MediatR configuration

namespace AgroLink.Infrastructure.Tests; // Changed namespace

public abstract class TestBase
{
    protected AgroLinkDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AgroLinkDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AgroLinkDbContext(options);
    }

    protected ServiceProvider CreateServiceProvider(AgroLinkDbContext context)
    {
        var services = new ServiceCollection();

        // Add DbContext
        services.AddSingleton(context);

        // Add repositories
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IFarmRepository, FarmRepository>();
        services.AddScoped<IPaddockRepository, PaddockRepository>();
        services.AddScoped<ILotRepository, LotRepository>();
        services.AddScoped<IAnimalRepository, AnimalRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IAnimalOwnerRepository, AnimalOwnerRepository>();
        services.AddScoped<IChecklistRepository, ChecklistRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        // Add new CQRS-related repositories and services
        services.AddScoped<IPhotoRepository, PhotoRepository>(); // Explicitly use Application interface
        services.AddScoped<IMovementRepository, MovementRepository>(); // Explicitly use Application interface
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAwsS3Service, AwsS3Service>();
        services.AddScoped<IPasswordHasher, PasswordHasher>(); // Registered new IPasswordHasher

        // Add ChecklistService (as it still exists)
        services.AddScoped<ITokenExtractionService, TokenExtractionService>();

        // Add MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnimalDto).Assembly));

        return services.BuildServiceProvider();
    }

    protected async Task<Farm> CreateTestFarmAsync(
        AgroLinkDbContext context,
        string name = "Test Farm"
    )
    {
        var farm = new Farm
        {
            Name = name,
            Location = "Test Location",
            CreatedAt = DateTime.UtcNow,
        };

        context.Farms.Add(farm);
        await context.SaveChangesAsync();
        return farm;
    }

    protected async Task<Paddock> CreateTestPaddockAsync(
        AgroLinkDbContext context,
        int farmId,
        string name = "Test Paddock"
    )
    {
        var paddock = new Paddock
        {
            Name = name,
            FarmId = farmId,
            CreatedAt = DateTime.UtcNow,
        };

        context.Paddocks.Add(paddock);
        await context.SaveChangesAsync();
        return paddock;
    }

    protected async Task<Lot> CreateTestLotAsync(
        AgroLinkDbContext context,
        int paddockId,
        string name = "Test Lot"
    )
    {
        var lot = new Lot
        {
            Name = name,
            PaddockId = paddockId,
            Status = "Active",
            CreatedAt = DateTime.UtcNow,
        };

        context.Lots.Add(lot);
        await context.SaveChangesAsync();
        return lot;
    }

    protected async Task<Animal> CreateTestAnimalAsync(
        AgroLinkDbContext context,
        int lotId,
        string tag = "A001"
    )
    {
        var animal = new Animal
        {
            Tag = tag,
            Name = "Test Animal",
            Color = "Brown",
            Breed = "Holstein",
            Sex = "Female",
            BirthDate = DateTime.UtcNow.AddYears(-2),
            LotId = lotId,
            CreatedAt = DateTime.UtcNow,
        };

        context.Animals.Add(animal);
        await context.SaveChangesAsync();
        return animal;
    }

    protected async Task<Owner> CreateTestOwnerAsync(
        AgroLinkDbContext context,
        string name = "Test Owner"
    )
    {
        var owner = new Owner
        {
            Name = name,
            Phone = "123-456-7890",
            CreatedAt = DateTime.UtcNow,
        };

        context.Owners.Add(owner);
        await context.SaveChangesAsync();
        return owner;
    }

    protected async Task<User> CreateTestUserAsync(
        AgroLinkDbContext context,
        string email = "test@example.com"
    )
    {
        var user = new User
        {
            Name = "Test User",
            Email = email,
            PasswordHash = "hashed_password",
            Role = "Admin",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }
}
