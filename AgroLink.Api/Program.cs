using System.Text;
using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Application.Services;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using AgroLink.Infrastructure.Repositories;
using AgroLink.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.CustomSchemaIds(type => type.FullName);
});

// Database
builder.Services.AddDbContext<AgroLinkDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Repositories
builder.Services.AddScoped<IRepository<Farm>, Repository<Farm>>();
builder.Services.AddScoped<IRepository<Paddock>, Repository<Paddock>>();
builder.Services.AddScoped<IRepository<Lot>, Repository<Lot>>();
builder.Services.AddScoped<IRepository<Animal>, Repository<Animal>>();
builder.Services.AddScoped<IRepository<Owner>, Repository<Owner>>();
builder.Services.AddScoped<IRepository<AnimalOwner>, Repository<AnimalOwner>>();
builder.Services.AddScoped<IRepository<Checklist>, Repository<Checklist>>();
builder.Services.AddScoped<IRepository<ChecklistItem>, Repository<ChecklistItem>>();
builder.Services.AddScoped<IRepository<Movement>, Repository<Movement>>();
builder.Services.AddScoped<IRepository<Photo>, Repository<Photo>>();
builder.Services.AddScoped<IRepository<User>, Repository<User>>();

// Specific Repositories
builder.Services.AddScoped<IFarmRepository, FarmRepository>();
builder.Services.AddScoped<IPaddockRepository, PaddockRepository>();
builder.Services.AddScoped<ILotRepository, LotRepository>();
builder.Services.AddScoped<IAnimalRepository, AnimalRepository>();
builder.Services.AddScoped<IOwnerRepository, OwnerRepository>();
builder.Services.AddScoped<IAnimalOwnerRepository, AnimalOwnerRepository>();
builder.Services.AddScoped<IChecklistRepository, ChecklistRepository>();
builder.Services.AddScoped<IMovementRepository, MovementRepository>();
builder.Services.AddScoped<IPhotoRepository, PhotoRepository>();
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuthRepository, AuthRepository>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IAwsS3Service, AwsS3Service>();
builder.Services.AddScoped<IPasswordHasher, PasswordHasher>(); // Registered new IPasswordHasher
builder.Services.AddScoped<ITokenExtractionService, TokenExtractionService>();

// MediatR
builder.Services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnimalDto).Assembly));

// JWT Authentication
var jwtKey =
    builder.Configuration["Jwt:Key"] ?? "your-super-secret-key-that-is-at-least-32-characters-long";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "AgroLink";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "AgroLink";

builder
    .Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.ASCII.GetBytes(jwtKey)),
            ValidateIssuer = true,
            ValidIssuer = jwtIssuer,
            ValidateAudience = true,
            ValidAudience = jwtAudience,
            ClockSkew = TimeSpan.Zero,
        };
    });

builder.Services.AddAuthorization();

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

// Configure the HTTP request pipeline.
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
