using System.Text.Json.Serialization;
using AgroLink.Api.Filters;
using AgroLink.Api.Middleware;
using AgroLink.Api.Security;
using AgroLink.Api.Services;
using AgroLink.Application;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using AgroLink.Infrastructure;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

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
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<InternalJobKeyFilter>();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
});

// Layer Dependencies
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// Caching & Security
builder.Services.AddMemoryCache();
builder.Services.AddSingleton<IAuthorizationHandler, FarmRoleHandler>();

// Firebase Authentication — validates Firebase ID tokens using Google's public JWKS
var firebaseProjectId =
    builder.Configuration["Firebase:ProjectId"]
    ?? throw new InvalidOperationException("Firebase:ProjectId is required in configuration.");

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.Authority = $"https://securetoken.google.com/{firebaseProjectId}";
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = $"https://securetoken.google.com/{firebaseProjectId}",
            ValidateAudience = true,
            ValidAudience = firebaseProjectId,
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
app.UseMiddleware<FirebaseUserMiddleware>();
app.UseAuthorization();
app.MapControllers();

app.Run();

public partial class Program { }
