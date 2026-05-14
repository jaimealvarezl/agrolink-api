using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Data.Interceptors;
using AgroLink.Infrastructure.Repositories;
using AgroLink.Infrastructure.Services;
using FirebaseAdmin;
using Google.Apis.Auth.OAuth2;
using Google.Cloud.Storage.V1;
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
        return services.AddInfrastructureCore(configuration).AddInfrastructureHttpClients();
    }

    public static IServiceCollection AddInfrastructureCore(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        // Database
        services.AddSingleton<SearchTextInterceptor>();

        var connectionString =
            configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is required."
            );

        services.AddDbContext<AgroLinkDbContext>(
            (sp, options) =>
                options
                    .UseNpgsql(
                        connectionString,
                        o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
                    )
                    .AddInterceptors(sp.GetRequiredService<SearchTextInterceptor>())
        );

        // Google Cloud Storage
        services.AddSingleton<StorageClient>(_ => StorageClient.Create());
        services.AddSingleton<UrlSigner>(_ =>
            UrlSigner.FromCredential(GoogleCredential.GetApplicationDefault())
        );

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
        services.AddScoped<IRepository<User>, Repository<User>>();
        services.AddScoped<IRepository<ClinicalCase>, Repository<ClinicalCase>>();
        services.AddScoped<IRepository<ClinicalCaseEvent>, Repository<ClinicalCaseEvent>>();
        services.AddScoped<
            IRepository<ClinicalRecommendation>,
            Repository<ClinicalRecommendation>
        >();
        services.AddScoped<IRepository<ClinicalAlert>, Repository<ClinicalAlert>>();
        services.AddScoped<IRepository<Medication>, Repository<Medication>>();
        services.AddScoped<IRepository<MedicationRule>, Repository<MedicationRule>>();
        services.AddScoped<IRepository<MedicationImage>, Repository<MedicationImage>>();
        services.AddScoped<
            IRepository<TelegramInboundEventLog>,
            Repository<TelegramInboundEventLog>
        >();
        services.AddScoped<
            IRepository<TelegramOutboundMessage>,
            Repository<TelegramOutboundMessage>
        >();

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
        services.AddScoped<IAnimalNoteRepository, AnimalNoteRepository>();
        services.AddScoped<IAnimalRetirementRepository, AnimalRetirementRepository>();
        services.AddScoped<IOwnerBrandRepository, OwnerBrandRepository>();
        services.AddScoped<IAnimalBrandRepository, AnimalBrandRepository>();
        services.AddScoped<IFarmMemberRepository, FarmMemberRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IClinicalCaseRepository, ClinicalCaseRepository>();
        services.AddScoped<IClinicalCaseEventRepository, ClinicalCaseEventRepository>();
        services.AddScoped<IClinicalRecommendationRepository, ClinicalRecommendationRepository>();
        services.AddScoped<ITelegramInboundEventLogRepository, TelegramInboundEventLogRepository>();
        services.AddScoped<ITelegramOutboundMessageRepository, TelegramOutboundMessageRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Firebase Admin SDK — initialized once per process using Application Default Credentials.
        // Locally: set GOOGLE_APPLICATION_CREDENTIALS to a service account JSON path.
        // On Cloud Run: uses the service account attached to the instance.
        if (FirebaseApp.DefaultInstance == null)
        {
            try
            {
                FirebaseApp.Create(
                    new AppOptions
                    {
                        Credential = GoogleCredential.GetApplicationDefault(),
                        ProjectId = configuration["Firebase:ProjectId"],
                    }
                );
            }
            catch (Exception)
            {
                // No credentials available (local dev without service account).
                // JWT validation via JwtBearer+JWKS still works; Admin SDK features
                // (revocation checks, custom tokens) require credentials at runtime.
            }
        }

        // Infrastructure Services
        services.AddMemoryCache();
        services.AddScoped<IFarmRosterService, FarmRosterService>();
        services.AddScoped<IEntityResolutionService, EntityResolutionService>();
        services.AddScoped<IAuthRepository, AuthRepository>();
        services.AddScoped<IStorageService, GcsStorageService>();
        services.AddScoped<IStoragePathProvider, StoragePathProvider>();

        // External worker operations are dispatched directly to the registered services.
        // Cloud Run has unrestricted internet access so no Lambda/HTTP proxy is needed.
        services.AddScoped<IExternalApiWorkerClient, DirectExternalApiWorkerClient>();

        return services;
    }

    public static IServiceCollection AddInfrastructureHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient<ITelegramGateway, TelegramGateway>();
        services.AddHttpClient<
            IClinicalMedicationAdvisorService,
            OpenAiClinicalMedicationAdvisorService
        >();
        services.AddHttpClient<
            IClinicalAudioTranscriptionService,
            OpenAiClinicalAudioTranscriptionService
        >();
        services.AddHttpClient<IClinicalTextToSpeechService, OpenAiClinicalTextToSpeechService>();
        services.AddHttpClient<IAnimalHealthAnalysisService, OpenAiAnimalHealthAnalysisService>();

        return services;
    }
}
