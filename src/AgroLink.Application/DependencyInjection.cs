using AgroLink.Application.Common.Services;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Features.Animals.Validators;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace AgroLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnimalDto).Assembly));
        services.AddScoped<ITokenExtractionService, TokenExtractionService>();
        services.AddScoped<IOwnershipValidator, OwnershipValidator>();

        return services;
    }
}
