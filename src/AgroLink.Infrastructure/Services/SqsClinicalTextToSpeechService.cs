using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Features.ExternalWorkers.Models;
using AgroLink.Application.Interfaces;

namespace AgroLink.Infrastructure.Services;

public class SqsClinicalTextToSpeechService(IExternalApiWorkerClient client)
    : IClinicalTextToSpeechService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public async Task<ClinicalTextToSpeechResult?> SynthesizeAsync(
        ClinicalTextToSpeechRequest request,
        CancellationToken ct
    )
    {
        var workerRequest = new ExternalWorkerRequest(
            Guid.NewGuid().ToString(),
            ExternalWorkerOperations.SynthesizeSpeech,
            JsonSerializer.SerializeToElement(request, JsonOptions)
        );

        var response = await client.ExecuteAsync(workerRequest, ct);

        if (!response.Success)
        {
            throw new InvalidOperationException($"SynthesizeSpeech failed: {response.Error}");
        }

        var dto =
            response.Result?.Deserialize<SynthesizeSpeechResultDto>(JsonOptions)
            ?? throw new InvalidOperationException("SynthesizeSpeech returned a null result.");

        return new ClinicalTextToSpeechResult
        {
            Success = dto.Success,
            FileName = dto.FileName,
            MimeType = dto.MimeType,
            ProviderResponse = dto.ProviderResponse,
            AudioContent = Convert.FromBase64String(dto.Base64AudioContent),
        };
    }

    private record SynthesizeSpeechResultDto(
        bool Success,
        string FileName,
        string MimeType,
        string ProviderResponse,
        string Base64AudioContent
    );
}
