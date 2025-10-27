using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace AgroLink.Tests;

public static class TestConfiguration
{
    public static IConfiguration CreateConfiguration()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] =
                        "Server=localhost;Database=agrolink_test;Username=postgres;Password=password",
                    ["Jwt:Key"] = "test-secret-key-that-is-long-enough-for-hmac-sha256-this-is-128-bits-minimum",
                    ["Jwt:Issuer"] = "AgroLink-Test",
                    ["Jwt:Audience"] = "AgroLink-Test",
                    ["Jwt:ExpiryMinutes"] = "60",
                    ["AWS:AccessKey"] = "test-access-key",
                    ["AWS:SecretKey"] = "test-secret-key",
                    ["AWS:Region"] = "us-east-1",
                    ["AWS:S3BucketName"] = "agrolink-test-bucket",
                }
            )
            .Build();

        return configuration;
    }

    public static AgroLinkDbContext CreateTestDbContext()
    {
        var options = new DbContextOptionsBuilder<AgroLinkDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        return new AgroLinkDbContext(options);
    }
}
