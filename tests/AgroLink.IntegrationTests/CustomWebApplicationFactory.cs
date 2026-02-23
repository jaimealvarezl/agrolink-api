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
