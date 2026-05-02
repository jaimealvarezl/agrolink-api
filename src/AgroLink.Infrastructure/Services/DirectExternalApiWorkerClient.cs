using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

/// <summary>
///     Dispatches external worker operations directly to the registered services in-process.
///     Replaces the Lambda proxy pattern: Cloud Run has unrestricted internet access so no proxy is needed.
/// </summary>
public class DirectExternalApiWorkerClient(
    IServiceProvider serviceProvider,
    ILogger<DirectExternalApiWorkerClient> logger
) : IExternalApiWorkerClient
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ExternalWorkerResponse> ExecuteAsync(
        ExternalWorkerRequest request,
        CancellationToken ct
    )
    {
        logger.LogDebug(
            "Executing worker operation {Operation} with correlation {CorrelationId}",
            request.Operation,
            request.CorrelationId
        );

        try
        {
            return await DispatchAsync(request, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Worker operation {Operation} failed. CorrelationId: {CorrelationId}",
                request.Operation,
                request.CorrelationId
            );
            throw;
        }
    }

    private async Task<ExternalWorkerResponse> DispatchAsync(
        ExternalWorkerRequest request,
        CancellationToken ct
    )
    {
        using var scope = serviceProvider.CreateScope();
        var services = scope.ServiceProvider;

        return request.Operation switch
        {
            ExternalWorkerOperations.GetMedicationAdvice => await HandleGetMedicationAdviceAsync(
                request,
                services,
                ct
            ),
            ExternalWorkerOperations.TranscribeAudio => await HandleTranscribeAudioAsync(
                request,
                services,
                ct
            ),
            ExternalWorkerOperations.SynthesizeSpeech => await HandleSynthesizeSpeechAsync(
                request,
                services,
                ct
            ),
            ExternalWorkerOperations.SendTelegramText => await HandleSendTelegramTextAsync(
                request,
                services,
                ct
            ),
            ExternalWorkerOperations.SendTelegramVoice => await HandleSendTelegramVoiceAsync(
                request,
                services,
                ct
            ),
            ExternalWorkerOperations.DownloadTelegramFile => await HandleDownloadTelegramFileAsync(
                request,
                services,
                ct
            ),
            ExternalWorkerOperations.TranscribeVoiceAudio => await HandleTranscribeVoiceAudioAsync(
                request,
                services,
                ct
            ),
            ExternalWorkerOperations.ExtractVoiceIntent => await HandleExtractVoiceIntentAsync(
                request,
                services,
                ct
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

    private static T Deserialize<T>(JsonElement payload)
    {
        return payload.Deserialize<T>(JsonOptions)
            ?? throw new InvalidOperationException("Invalid operation payload.");
    }

    private static JsonElement ToElement(object value)
    {
        return JsonSerializer.SerializeToElement(value, JsonOptions);
    }

    private static async Task<ExternalWorkerResponse> HandleGetMedicationAdviceAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<IClinicalMedicationAdvisorService>();
        var payload = Deserialize<ClinicalMedicationAdviceRequest>(request.Payload);
        var result = await svc.GetAdviceAsync(payload, ct);
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            true,
            ToElement(result),
            null
        );
    }

    private static async Task<ExternalWorkerResponse> HandleTranscribeAudioAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<IClinicalAudioTranscriptionService>();
        var payload = Deserialize<TranscribeAudioPayload>(request.Payload);
        var audioBytes = Convert.FromBase64String(payload.Base64AudioContent);
        var text = await svc.TranscribeAsync(
            new ClinicalAudioTranscriptionRequest(audioBytes, payload.FileName, payload.MimeType),
            ct
        );
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            true,
            ToElement(new { text }),
            null
        );
    }

    private static async Task<ExternalWorkerResponse> HandleSynthesizeSpeechAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<IClinicalTextToSpeechService>();
        var payload = Deserialize<ClinicalTextToSpeechRequest>(request.Payload);
        var result = await svc.SynthesizeAsync(payload, ct);
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

    private static async Task<ExternalWorkerResponse> HandleSendTelegramTextAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<ITelegramGateway>();
        var payload = Deserialize<SendTelegramTextPayload>(request.Payload);
        var result = await svc.SendTextMessageAsync(payload.ChatId, payload.Text, ct);
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            result.Success,
            ToElement(result),
            null
        );
    }

    private static async Task<ExternalWorkerResponse> HandleSendTelegramVoiceAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<ITelegramGateway>();
        var payload = Deserialize<SendTelegramVoicePayload>(request.Payload);
        var audioBytes = Convert.FromBase64String(payload.Base64AudioContent);
        var result = await svc.SendVoiceMessageAsync(
            payload.ChatId,
            audioBytes,
            payload.FileName,
            payload.MimeType,
            payload.Caption,
            ct
        );
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            result.Success,
            ToElement(result),
            null
        );
    }

    private static async Task<ExternalWorkerResponse> HandleDownloadTelegramFileAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<ITelegramGateway>();
        var payload = Deserialize<DownloadTelegramFilePayload>(request.Payload);
        var result = await svc.DownloadFileAsync(payload.FileId, ct);
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            result.Success,
            ToElement(
                new
                {
                    Base64Content = result.Content is not null
                        ? Convert.ToBase64String(result.Content)
                        : null,
                    result.FilePath,
                    result.ContentType,
                }
            ),
            result.Success ? null : result.ProviderResponse
        );
    }

    private static async Task<ExternalWorkerResponse> HandleTranscribeVoiceAudioAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<IVoiceTranscriptionService>();
        var payload = Deserialize<TranscribeVoiceAudioPayload>(request.Payload);
        var audioBytes = Convert.FromBase64String(payload.Base64AudioContent);
        var text = await svc.TranscribeAsync(
            audioBytes,
            payload.FileName,
            payload.MimeType,
            payload.Language,
            ct
        );
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            true,
            ToElement(new { text = text ?? string.Empty }),
            null
        );
    }

    private static async Task<ExternalWorkerResponse> HandleExtractVoiceIntentAsync(
        ExternalWorkerRequest request,
        IServiceProvider services,
        CancellationToken ct
    )
    {
        var svc = services.GetRequiredService<IVoiceIntentService>();
        var payload = Deserialize<ExtractVoiceIntentPayload>(request.Payload);
        var intentJson = await svc.ExtractIntentAsync(payload.Transcript, ct);

        if (string.IsNullOrWhiteSpace(intentJson))
        {
            return new ExternalWorkerResponse(
                request.CorrelationId,
                request.Operation,
                false,
                null,
                "Intent extraction returned empty response."
            );
        }

        using var doc = JsonDocument.Parse(intentJson);
        return new ExternalWorkerResponse(
            request.CorrelationId,
            request.Operation,
            true,
            doc.RootElement.Clone(),
            null
        );
    }
}
