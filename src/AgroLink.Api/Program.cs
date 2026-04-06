using System.Text;
using System.Text.Json.Serialization;
using AgroLink.Api;
using AgroLink.Api.Filters;
using AgroLink.Api.Security;
using AgroLink.Api.Services;
using AgroLink.Application;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using AgroLink.Infrastructure;
using Amazon.SQS;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Load configuration from Secrets Manager if available
if (!builder.Environment.IsEnvironment("Testing"))
{
    await SecretsManagerHelper.LoadSecretsAsync(builder);
}

// Add services to the container.
builder
    .Services.AddControllers(options =>
    {
        options.Filters.Add<GlobalExceptionFilter>();
    })
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
});

// AWS Services
builder.Services.AddAWSService<IAmazonSQS>();

// Layer Dependencies
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Caching & Security
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAuthorizationHandler, FarmRoleHandler>();

// JWT Authentication
var jwtKey =
    builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key is missing in configuration.");
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgroLink";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AgroLink";

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization(options =>
{
    // Hierarchical Policies
    options.AddPolicy(
        "FarmOwnerOnly",
        policy => policy.AddRequirements(new FarmRoleRequirement(FarmMemberRoles.Owner))
    );

    options.AddPolicy(
        "FarmAdminAccess",
        policy => policy.AddRequirements(new FarmRoleRequirement(FarmMemberRoles.Admin))
    );

    options.AddPolicy(
        "FarmEditorAccess",
        policy => policy.AddRequirements(new FarmRoleRequirement(FarmMemberRoles.Editor))
    );

    options.AddPolicy(
        "FarmViewerAccess",
        policy => policy.AddRequirements(new FarmRoleRequirement(FarmMemberRoles.Viewer))
    );
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        policy =>
        {
            policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader();
        }
    );
});

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
