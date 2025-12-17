using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Repositories;
using AgroLink.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AgroLink.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Database
        services.AddDbContext<AgroLinkDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection"))
        );

        // Generic Repositories
        services.AddScoped<IRepository<Farm>, Repository<Farm>>();
        services.AddScoped<IRepository<Paddock>, Repository<Paddock>>();
        services.AddScoped<IRepository<Lot>, Repository<Lot>>();
        services.AddScoped<IRepository<Animal>, Repository<Animal>>();
        services.AddScoped<IRepository<Owner>, Repository<Owner>>();
        services.AddScoped<IRepository<AnimalOwner>, Repository<AnimalOwner>>();
        services.AddScoped<IRepository<Checklist>, Repository<Checklist>>();
        services.AddScoped<IRepository<ChecklistItem>, Repository<ChecklistItem>>();
        services.AddScoped<IRepository<Movement>, Repository<Movement>>();
        services.AddScoped<IRepository<Photo>, Repository<Photo>>();
        services.AddScoped<IRepository<User>, Repository<User>>();

        // Specific Repositories
        services.AddScoped<IFarmRepository, FarmRepository>();
        services.AddScoped<IPaddockRepository, PaddockRepository>();
        services.AddScoped<ILotRepository, LotRepository>();
        services.AddScoped<IAnimalRepository, AnimalRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IAnimalOwnerRepository, AnimalOwnerRepository>();
        services.AddScoped<IChecklistRepository, ChecklistRepository>();
        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Infrastructure Services
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IAwsS3Service, AwsS3Service>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
