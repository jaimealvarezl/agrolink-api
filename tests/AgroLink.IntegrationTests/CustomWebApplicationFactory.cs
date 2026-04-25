using AgroLink.Application.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;

namespace AgroLink.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .WithDatabase("agrolink_test")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string GetConnectionString()
    {
        return _dbContainer.GetConnectionString();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Force Testing environment
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "Jwt:Key", "your-super-secret-key-that-is-at-least-32-characters-long" },
                        { "Jwt:Issuer", "AgroLink" },
                        { "Jwt:Audience", "AgroLink" },
                        {
                            "ConnectionStrings:DefaultConnection",
                            _dbContainer.GetConnectionString()
                        },
                        { "AWS:Region", "us-east-1" }, // Prevent AWS SDK errors if any
                    }
                );
            }
        );

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AgroLinkDbContext>)
            );

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add PostgreSQL Database for testing using the container connection string
            services.AddDbContext<AgroLinkDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            // Build the service provider
            var sp = services.BuildServiceProvider();

            // Create a scope to obtain a reference to the database context
            using (var scope = sp.CreateScope())
            {
                var scopedServices = scope.ServiceProvider;
                var db = scopedServices.GetRequiredService<AgroLinkDbContext>();

                // Ensure the database is created and migrations applied
                db.Database.Migrate();
            }

            // Replace S3 with a no-op fake so tests don't need real AWS credentials
            services.AddScoped<IStorageService, FakeStorageService>();

            // Replace SQS queue with a no-op fake so tests don't need real AWS credentials
            services.AddScoped<IVoiceCommandQueue, FakeVoiceCommandQueue>();
        });
    }

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();
    }

    public new async Task DisposeAsync()
    {
        await _dbContainer.StopAsync();
    }
}

internal class FakeStorageService : IStorageService
{
    public Task UploadFileAsync(
        string key,
        Stream fileStream,
        string contentType,
        long contentLength
    )
    {
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string key)
    {
        return Task.CompletedTask;
    }

    public string GetFileUrl(string key)
    {
        return $"https://fake-storage/{key}";
    }

    public string GetPresignedUrl(string key, TimeSpan expiration)
    {
        return $"https://fake-storage/{key}";
    }

    public string GetKeyFromUrl(string url)
    {
        return url.Replace("https://fake-storage/", string.Empty);
    }

    public Task<byte[]?> GetFileBytesAsync(
        string key,
        CancellationToken cancellationToken = default
    )
    {
        return Task.FromResult<byte[]?>(null);
    }
}

internal class FakeVoiceCommandQueue : IVoiceCommandQueue
{
    public Task EnqueueAsync(Guid jobId, int farmId, int userId, CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}
