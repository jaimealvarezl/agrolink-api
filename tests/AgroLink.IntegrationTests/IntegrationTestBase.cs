using System.IdentityModel.Tokens.Jwt;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using Respawn;

namespace AgroLink.IntegrationTests;

[TestFixture]
public abstract class IntegrationTestBase
{
    [OneTimeSetUp]
    public async Task GlobalSetup()
    {
        Factory = new CustomWebApplicationFactory<Program>();
        await Factory.InitializeAsync();
        _connectionString = Factory.GetConnectionString();

        using (var scope = Factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<AgroLinkDbContext>();
            // Ensures migrations ran via ConfigureWebHost
        }

        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        _respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,
                SchemasToInclude = new[] { "public" },
            }
        );
    }

    [SetUp]
    public async Task Setup()
    {
        Client = Factory.CreateClient();
        Scope = Factory.Services.CreateScope();
        DbContext = Scope.ServiceProvider.GetRequiredService<AgroLinkDbContext>();

        await ResetDatabaseAsync();
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

    protected CustomWebApplicationFactory<Program> Factory = null!;
    protected HttpClient Client = null!;
    protected IServiceScope Scope = null!;
    protected AgroLinkDbContext DbContext = null!;
    private Respawner _respawner = null!;
    private string _connectionString = null!;

    private async Task ResetDatabaseAsync()
    {
        await using var connection = new NpgsqlConnection(_connectionString);
        await connection.OpenAsync();
        await _respawner.ResetAsync(connection);
    }

    /// <summary>
    ///     Sets the Authorization header with a test token whose sub/email/name claims
    ///     match the given User. FirebaseUserMiddleware will look up the user by FirebaseUid.
    ///     The user must already be persisted in DbContext before calling this.
    /// </summary>
    protected void Authenticate(User user)
    {
        if (user.FirebaseUid == null)
        {
            user.FirebaseUid = $"test-uid-{user.Id}";
            DbContext.Users.Update(user);
            DbContext.SaveChanges();
        }

        var token = CreateTestToken(user.FirebaseUid, user.Email, user.Name);
        Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    private static string CreateTestToken(string firebaseUid, string email, string name)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(CustomWebApplicationFactory<Program>.TestJwtKey)
        );

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("sub", firebaseUid),
                new Claim("email", email),
                new Claim("name", name),
            ]),
            Expires = DateTime.UtcNow.AddHours(1),
            SigningCredentials = new SigningCredentials(
                key,
                SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }
}
