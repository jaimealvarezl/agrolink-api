using AgroLink.Application;
using AgroLink.Application.Features.VoiceCommands.Commands.DeleteStaleVoiceCommandJobs;
using AgroLink.Infrastructure;
using Amazon.Lambda.Core;
using MediatR;

namespace AgroLink.Workers;

public class CleanupFunction
{
    private readonly ILogger<CleanupFunction> _logger;
    private readonly IServiceProvider _serviceProvider;

    public CleanupFunction()
    {
        var builder = WebApplication.CreateBuilder();

        if (!builder.Environment.IsEnvironment("Testing"))
        {
            SecretsManagerHelper.LoadSecretsAsync(builder.Configuration).GetAwaiter().GetResult();
        }

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        var app = builder.Build();
        _serviceProvider = app.Services;
        _logger = _serviceProvider.GetRequiredService<ILogger<CleanupFunction>>();
    }

    public async Task FunctionHandler(ILambdaContext context)
    {
        _logger.LogInformation("Starting VoiceCommandJob cleanup");

        using var scope = _serviceProvider.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

        var deleted = await mediator.Send(new DeleteStaleVoiceCommandJobsCommand());

        _logger.LogInformation("Cleanup complete. Deleted {Count} stale job(s)", deleted);
    }
}
