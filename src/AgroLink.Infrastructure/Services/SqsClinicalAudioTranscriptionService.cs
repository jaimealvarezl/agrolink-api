using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class SqsClinicalAudioTranscriptionService(IExternalApiWorkerClient client)
    : IClinicalAudioTranscriptionService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<string?> TranscribeAsync(
        ClinicalAudioTranscriptionRequest request,
        CancellationToken ct
    )
    {
        var payload = new TranscribeAudioPayload(
            Convert.ToBase64String(request.AudioContent),
            request.FileName,
            request.MimeType
        );

        var workerRequest = new ExternalWorkerRequest(
            Guid.NewGuid().ToString(),
            ExternalWorkerOperations.TranscribeAudio,
            JsonSerializer.SerializeToElement(payload, JsonOptions)
        );

        var response = await client.ExecuteAsync(workerRequest, ct);

        if (!response.Success)
        {
            throw new InvalidOperationException($"TranscribeAudio failed: {response.Error}");
        }

        return response.Result?.GetProperty("text").GetString()
            ?? throw new InvalidOperationException("TranscribeAudio returned a null result.");
    }
}
