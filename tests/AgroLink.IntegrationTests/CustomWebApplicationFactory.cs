using System.Text;
using AgroLink.Application.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Testcontainers.PostgreSql;

namespace AgroLink.IntegrationTests;

public class CustomWebApplicationFactory<TProgram> : WebApplicationFactory<TProgram>
    where TProgram : class
{
    internal const string TestJwtKey = "test-firebase-key-that-is-at-least-32-characters-long";

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
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration(
            (context, config) =>
            {
                config.AddInMemoryCollection(
                    new Dictionary<string, string?>
                    {
                        { "Firebase:ProjectId", "test-project" },
                        {
                            "ConnectionStrings:DefaultConnection",
                            _dbContainer.GetConnectionString()
                        },
                        { "AWS:Region", "us-east-1" },
                    }
                );
            }
        );

        builder.ConfigureServices(services =>
        {
            // Replace Firebase JWKS validation with a test HMAC key so tests
            // don't need a real Firebase project. Tokens still carry sub/email/name
            // claims and pass through FirebaseUserMiddleware unchanged.
            services.PostConfigure<JwtBearerOptions>(
                JwtBearerDefaults.AuthenticationScheme,
                options =>
                {
                    options.Authority = null;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuerSigningKey = true,
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(TestJwtKey)
                        ),
                        ValidateIssuer = false,
                        ValidateAudience = false,
                    };
                }
            );

            var descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AgroLinkDbContext>)
            );

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AgroLinkDbContext>(options =>
            {
                options.UseNpgsql(_dbContainer.GetConnectionString());
            });

            var sp = services.BuildServiceProvider();

            using var scope = sp.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AgroLinkDbContext>();
            db.Database.Migrate();

            services.AddScoped<IStorageService, FakeStorageService>();
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
        long contentLength,
        CancellationToken cancellationToken = default
    )
    {
        return Task.CompletedTask;
    }

    public Task DeleteFileAsync(string key, CancellationToken cancellationToken = default)
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
