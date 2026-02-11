using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Repositories;
using AgroLink.Infrastructure.Services;
using Amazon.S3;
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
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            )
        );

        // AWS S3 / MinIO Configuration
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var config = sp.GetRequiredService<IConfiguration>();
            var awsSection = config.GetSection("AWS");
            var serviceUrl = awsSection["ServiceUrl"];
            var accessKey = awsSection["AccessKey"];
            var secretKey = awsSection["SecretKey"];
            bool.TryParse(awsSection["ForcePathStyle"], out var forcePathStyle);

            var s3Config = new AmazonS3Config();
            if (!string.IsNullOrEmpty(serviceUrl))
            {
                s3Config.ServiceURL = serviceUrl;
                s3Config.ForcePathStyle = forcePathStyle;
            }

            if (!string.IsNullOrEmpty(accessKey) && !string.IsNullOrEmpty(secretKey))
            {
                return new AmazonS3Client(accessKey, secretKey, s3Config);
            }

            return new AmazonS3Client(s3Config);
        });

        // Generic Repositories
        services.AddScoped<IRepository<Farm>, Repository<Farm>>();
        services.AddScoped<IRepository<Paddock>, Repository<Paddock>>();
        services.AddScoped<IRepository<Lot>, Repository<Lot>>();
        services.AddScoped<IRepository<Animal>, Repository<Animal>>();
        services.AddScoped<IRepository<AnimalPhoto>, Repository<AnimalPhoto>>();
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
        services.AddScoped<IAnimalPhotoRepository, AnimalPhotoRepository>();
        services.AddScoped<IOwnerRepository, OwnerRepository>();
        services.AddScoped<IAnimalOwnerRepository, AnimalOwnerRepository>();
        services.AddScoped<IMovementRepository, MovementRepository>();
        services.AddScoped<IChecklistRepository, ChecklistRepository>();
        services.AddScoped<IFarmMemberRepository, FarmMemberRepository>();
        services.AddScoped<IPhotoRepository, PhotoRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Infrastructure Services
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IStorageService, S3StorageService>();
        services.AddScoped<IStoragePathProvider, StoragePathProvider>();
        services.AddScoped<IPasswordHasher, PasswordHasher>();

        return services;
    }
}
