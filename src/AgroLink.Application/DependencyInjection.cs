using AgroLink.Application.Common.Services;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Validators;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using QuestPDF;
using QuestPDF.Infrastructure;

namespace AgroLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        Settings.License = LicenseType.Community;

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnimalDto).Assembly));
        services.AddScoped<ITokenExtractionService, TokenExtractionService>();
        services.AddScoped<IOwnershipValidator, OwnershipValidator>();
        services.AddScoped<IClinicalExtractionService, HeuristicClinicalExtractionService>();
        services.AddScoped<IFarmAnimalResolver, FarmAnimalResolver>();

        return services;
    }
}
