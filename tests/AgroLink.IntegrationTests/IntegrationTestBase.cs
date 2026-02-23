using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using Respawn;
using Respawn.Graph;
using Npgsql;

namespace AgroLink.IntegrationTests;

[TestFixture]
public abstract class IntegrationTestBase
{
    protected CustomWebApplicationFactory<Program> Factory = null!;
    protected HttpClient Client = null!;
    protected IServiceScope Scope = null!;
    protected AgroLinkDbContext DbContext = null!;
    private Respawner _respawner = null!;
    private string _connectionString = null!;

    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        Factory = new CustomWebApplicationFactory<Program>();
        await Factory.InitializeAsync();
        _connectionString = Factory.GetConnectionString();
        
        // Trigger host build and migrations by accessing Services
        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AgroLinkDbContext>();
            // This ensures migrations are run because ConfigureWebHost calls Migrate()
        }

        // Initialize Respawner
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(connection, new RespawnerOptions
        {
            DbAdapter = DbAdapter.Postgres,
            SchemasToInclude = new[] { "public" }
        });
    }

    [SetUp]
    public async Task Setup()
    {
        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AgroLinkDbContext>();
        
        // Reset database state before each test
        await ResetDatabaseAsync();
    }

    private async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    [TearDown]
    public void TearDown()
    {
        DbContext.Dispose();
        Scope.Dispose();
        Client.Dispose();
    }

    [OneTimeTearDown]
    public async Task GlobalTearDown()
    {
        await Factory.DisposeAsync();
    }

    protected void Authenticate(User user)
    {
        var jwtService = Scope.ServiceProvider.GetRequiredService<IJwtTokenService>();
        var token = jwtService.GenerateToken(user);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    protected void Authenticate(int userId, string email, string role, string name)
    {
        var user = new User
        {
            Id = userId,
            Email = email,
            Role = role,
            Name = name
        };
        Authenticate(user);
    }

    /* Remove GenerateJwtToken and original Authenticate */
}
