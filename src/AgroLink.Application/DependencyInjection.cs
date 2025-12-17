using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AgroLink.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(AnimalDto).Assembly));
        services.AddScoped<ITokenExtractionService, TokenExtractionService>();

        return services;
    }
}
