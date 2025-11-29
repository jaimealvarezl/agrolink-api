using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AgroLink.Infrastructure.Data;

/// <summary>
/// Design-time factory for EF Core migrations.
/// This allows EF Core tools to create the DbContext without loading the startup project.
/// </summary>
public class AgroLinkDbContextFactory : IDesignTimeDbContextFactory<AgroLinkDbContext>
{
    public AgroLinkDbContext CreateDbContext(string[] args)
    {
        // For design-time operations (migrations), we use a default connection string
        // The actual connection string will be provided at runtime
        var optionsBuilder = new DbContextOptionsBuilder<AgroLinkDbContext>();

        // Use a placeholder connection string for design-time
        // This will be replaced with the actual connection string when the bundle runs
        var connectionString =
            Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Database=agrolink;Username=postgres;Password=postgres";

        optionsBuilder.UseNpgsql(connectionString);

        return new AgroLinkDbContext(optionsBuilder.Options);
    }
}
