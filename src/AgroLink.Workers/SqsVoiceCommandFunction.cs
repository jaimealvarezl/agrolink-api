using System.Text.Json;
using AgroLink.Application;
using AgroLink.Application.Features.VoiceCommands.Commands.ProcessVoiceCommand;
using AgroLink.Infrastructure;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using MediatR;

namespace AgroLink.Workers;

public class SqsVoiceCommandFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<SqsVoiceCommandFunction> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SqsVoiceCommandFunction()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddApplication();
        builder.Services.AddInfrastructureCore(builder.Configuration);

        var app = builder.Build();
        _serviceProvider = app.Services;
        _logger = _serviceProvider.GetRequiredService<ILogger<SqsVoiceCommandFunction>>();
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        _logger.LogInformation(
            "Processing voice command SQS message {MessageId}",
            message.MessageId
        );

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var payload =
                JsonSerializer.Deserialize<VoiceCommandSqsPayload>(message.Body, JsonOptions)
                ?? throw new InvalidOperationException("Failed to deserialize SQS message body.");

            var command = new ProcessVoiceCommandCommand(
                payload.JobId,
                payload.FarmId,
                payload.UserId
            );
            await mediator.Send(command);

            _logger.LogInformation(
                "Voice command job {JobId} processed successfully.",
                payload.JobId
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Error processing voice command SQS message {MessageId}",
                message.MessageId
            );
            throw;
        }
    }

    private record VoiceCommandSqsPayload(Guid JobId, int FarmId, int UserId);
}
