using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Infrastructure.Services;
using Amazon.Lambda.Core;
using Amazon.Lambda.SQSEvents;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace AgroLink.Api;

public class ExternalApiWorkerFunction
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    private readonly ILogger<ExternalApiWorkerFunction> _logger;
    private readonly string _resultsQueueUrl;
    private readonly IServiceProvider _serviceProvider;

    public ExternalApiWorkerFunction()
    {
        var builder = WebApplication.CreateBuilder();

        builder.Services.AddHttpClient<ITelegramGateway, TelegramGateway>().RemoveAllLoggers();
        builder.Services.AddHttpClient<
            IClinicalMedicationAdvisorService,
            OpenAiClinicalMedicationAdvisorService
        >();
        builder.Services.AddHttpClient<
            IClinicalAudioTranscriptionService,
            OpenAiClinicalAudioTranscriptionService
        >();
        builder.Services.AddHttpClient<
            IClinicalTextToSpeechService,
            OpenAiClinicalTextToSpeechService
        >();
        builder.Services.AddSingleton<IAmazonSQS, AmazonSQSClient>();

        var app = builder.Build();
        _serviceProvider = app.Services;
        _logger = _serviceProvider.GetRequiredService<ILogger<ExternalApiWorkerFunction>>();
        _resultsQueueUrl = builder.Configuration["ExternalWorkers:ResultsQueueUrl"] ?? string.Empty;
    }

    internal ExternalApiWorkerFunction(IServiceProvider serviceProvider, string resultsQueueUrl)
    {
        _serviceProvider = serviceProvider;
        _logger = serviceProvider.GetRequiredService<ILogger<ExternalApiWorkerFunction>>();
        _resultsQueueUrl = resultsQueueUrl;
    }

    public async Task FunctionHandler(SQSEvent evnt, ILambdaContext context)
    {
        foreach (var message in evnt.Records)
        {
            await ProcessMessageAsync(message);
        }
    }

    private async Task ProcessMessageAsync(SQSEvent.SQSMessage message)
    {
        try
        {
            var request =
                JsonSerializer.Deserialize<ExternalWorkerRequest>(message.Body, JsonOptions)
                ?? throw new InvalidOperationException("Invalid request payload.");

            _logger.LogInformation(
                "Processing external worker request. CorrelationId: {CorrelationId}, Operation: {Operation}",
                request.CorrelationId,
                request.Operation
            );

            var response = await ExecuteOperationAsync(request);
            await PublishResultAsync(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "External worker failed processing message {MessageId}",
                message.MessageId
            );
            throw;
        }
    }

    private async Task<ExternalWorkerResponse> ExecuteOperationAsync(ExternalWorkerRequest request)
    {
        using var scope = _serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        return request.Operation switch
        {
            ExternalWorkerOperations.GetMedicationAdvice => await HandleGetMedicationAdviceAsync(
                request,
                services
            ),
            ExternalWorkerOperations.TranscribeAudio => await HandleTranscribeAudioAsync(
                request,
                services
            ),
            ExternalWorkerOperations.SynthesizeSpeech => await HandleSynthesizeSpeechAsync(
                request,
                services
            ),
            ExternalWorkerOperations.SendTelegramText => await HandleSendTelegramTextAsync(
                request,
                services
            ),
            ExternalWorkerOperations.SendTelegramVoice => await HandleSendTelegramVoiceAsync(
                request,
                services
            ),
            ExternalWorkerOperations.DownloadTelegramFile => await HandleDownloadTelegramFileAsync(
                request,
                services
            ),
            _ => new ExternalWorkerResponse(
                request.CorrelationId,
                request.Operation,
                false,
                null,
                $"Unsupported operation '{request.Operation}'."
            ),
        };
    }

    private static T DeserializePayload<T>(JsonElement payload)
    {
        return payload.Deserialize<T>(JsonOptions)
            ?? throw new InvalidOperationException("Invalid operation payload.");
    }

    private static JsonElement ToElement(object value)
    {
        return JsonSerializer.SerializeToElement(value, JsonOptions);
    }

    private async Task<ExternalWorkerResponse> HandleGetMedicationAdviceAsync(
        ExternalWorkerRequest request,
        IServiceProvider services
    )
    {
        var medicationService = services.GetRequiredService<IClinicalMedicationAdvisorService>();
        var payload = DeserializePayload<ClinicalMedicationAdviceRequest>(request.Payload);
        var result = await medicationService.GetAdviceAsync(payload, CancellationToken.None);
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            true,
            ToElement(result),
            null
        );
    }

    private async Task<ExternalWorkerResponse> HandleTranscribeAudioAsync(
        ExternalWorkerRequest request,
        IServiceProvider services
    )
    {
        var transcriptionService =
            services.GetRequiredService<IClinicalAudioTranscriptionService>();
        var payload = DeserializePayload<TranscribeAudioPayload>(request.Payload);
        var audioBytes = Convert.FromBase64String(payload.Base64AudioContent);
        var text = await transcriptionService.TranscribeAsync(
            new ClinicalAudioTranscriptionRequest(audioBytes, payload.FileName, payload.MimeType),
            CancellationToken.None
        );
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            true,
            ToElement(new { text }),
            null
        );
    }

    private async Task<ExternalWorkerResponse> HandleSynthesizeSpeechAsync(
        ExternalWorkerRequest request,
        IServiceProvider services
    )
    {
        var textToSpeechService = services.GetRequiredService<IClinicalTextToSpeechService>();
        var payload = DeserializePayload<ClinicalTextToSpeechRequest>(request.Payload);
        var result = await textToSpeechService.SynthesizeAsync(payload, CancellationToken.None);
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            true,
            ToElement(
                new
                {
                    result.Success,
                    result.FileName,
                    result.MimeType,
                    result.ProviderResponse,
                    Base64AudioContent = Convert.ToBase64String(result.AudioContent),
                }
            ),
            null
        );
    }

    private async Task<ExternalWorkerResponse> HandleSendTelegramTextAsync(
        ExternalWorkerRequest request,
        IServiceProvider services
    )
    {
        var telegramGateway = services.GetRequiredService<ITelegramGateway>();
        var payload = DeserializePayload<SendTelegramTextPayload>(request.Payload);
        var result = await telegramGateway.SendTextMessageAsync(
            payload.ChatId,
            payload.Text,
            CancellationToken.None
        );
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            result.Success,
            ToElement(result),
            null
        );
    }

    private async Task<ExternalWorkerResponse> HandleSendTelegramVoiceAsync(
        ExternalWorkerRequest request,
        IServiceProvider services
    )
    {
        var telegramGateway = services.GetRequiredService<ITelegramGateway>();
        var payload = DeserializePayload<SendTelegramVoicePayload>(request.Payload);
        var audioBytes = Convert.FromBase64String(payload.Base64AudioContent);
        var result = await telegramGateway.SendVoiceMessageAsync(
            payload.ChatId,
            audioBytes,
            payload.FileName,
            payload.MimeType,
            payload.Caption,
            CancellationToken.None
        );
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            result.Success,
            ToElement(result),
            null
        );
    }

    private async Task<ExternalWorkerResponse> HandleDownloadTelegramFileAsync(
        ExternalWorkerRequest request,
        IServiceProvider services
    )
    {
        var telegramGateway = services.GetRequiredService<ITelegramGateway>();
        var payload = DeserializePayload<DownloadTelegramFilePayload>(request.Payload);
        var result = await telegramGateway.DownloadFileAsync(
            payload.FileId,
            CancellationToken.None
        );
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            result.Success,
            ToElement(
                new
                {
                    Base64Content = Convert.ToBase64String(result.Content),
                    result.FilePath,
                    result.ContentType,
                }
            ),
            result.Success ? null : result.ProviderResponse
        );
    }

    private async Task PublishResultAsync(ExternalWorkerResponse response)
    {
        if (string.IsNullOrWhiteSpace(_resultsQueueUrl))
        {
            _logger.LogWarning(
                "ExternalWorkers:ResultsQueueUrl is not configured. Dropping result for correlation {CorrelationId}",
                response.CorrelationId
            );
            return;
        }

        var sqs = _serviceProvider.GetRequiredService<IAmazonSQS>();
        var body = JsonSerializer.Serialize(response, JsonOptions);
        await sqs.SendMessageAsync(
            new SendMessageRequest { QueueUrl = _resultsQueueUrl, MessageBody = body },
            CancellationToken.None
        );
    }
}
