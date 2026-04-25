using AgroLink.Application;
using AgroLink.Application.Features.ClinicalCases.Commands.ReceiveTelegramUpdate;
using AgroLink.Application.Interfaces;
using AgroLink.Infrastructure;
using AgroLink.Infrastructure.Services;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using Amazon.Lambda.SQSEvents;
using MediatR;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(DefaultLambdaJsonSerializer))]

namespace AgroLink.Workers;

public class SqsFunction
{
    private readonly ILogger<SqsFunction> _logger;
    private readonly IServiceProvider _serviceProvider;

    public SqsFunction()
    {
        var builder = WebApplication.CreateBuilder();

        if (!builder.Environment.IsEnvironment("Testing"))
        {
            SecretsManagerHelper.LoadSecretsAsync(builder.Configuration).GetAwaiter().GetResult();
        }

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);

        // Replace direct external-API services with Lambda-dispatching adapters.
        // The VPC has no NAT gateway so OpenAI/Telegram are unreachable from within the VPC.
        // All calls are proxied through ExternalApiWorkerFunction via direct Lambda invocation.
        builder.Services.AddScoped<ITelegramGateway, SqsTelegramGateway>();
        builder.Services.AddSingleton<
            IClinicalMedicationAdvisorService,
            SqsClinicalMedicationAdvisorService
        >();
        builder.Services.AddSingleton<
            IClinicalAudioTranscriptionService,
            SqsClinicalAudioTranscriptionService
        >();
        builder.Services.AddSingleton<
            IClinicalTextToSpeechService,
            SqsClinicalTextToSpeechService
        >();

        var app = builder.Build();
        _serviceProvider = app.Services;
        _logger = _serviceProvider.GetRequiredService<ILogger<SqsFunction>>();
    }

    /// <summary>
    ///     This method is called for every Lambda invocation from SQS.
    /// </summary>
    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message, context);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message, ILambdaContext context)
    {
        _logger.LogInformation("Processing SQS message ID: {MessageId}", message.MessageId);

        try
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();

            var command = new ReceiveTelegramUpdateCommand(message.Body);
            var result = await mediator.Send(command);

            _logger.LogInformation(
                "Successfully processed Telegram update from SQS. Status: {Status}, Message: {Message}",
                result.Status,
                result.Message
            );
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing SQS message {MessageId}", message.MessageId);
            // Re-throwing will cause SQS to retry based on the redrive policy
            throw;
        }
    }
}
