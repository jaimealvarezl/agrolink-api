using System.Net.Http.Headers;
using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class OpenAiClinicalAudioTranscriptionService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAiClinicalAudioTranscriptionService> logger
) : IClinicalAudioTranscriptionService
{
    private readonly string _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;

    private readonly string _baseUrl =
        configuration["OpenAI:TranscriptionBaseUrl"]
        ?? "https://api.openai.com/v1/audio/transcriptions";

    private readonly string _model = configuration["OpenAI:TranscriptionModel"] ?? "whisper-1";

    public async Task<string?> TranscribeAsync(
        ClinicalAudioTranscriptionRequest request,
        CancellationToken ct = default
    )
    {
        if (request.AudioContent.Length == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("OpenAI API key is not configured, audio cannot be transcribed.");
            return null;
        }

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(request.AudioContent);
        if (!string.IsNullOrWhiteSpace(request.MimeType))
        {
            fileContent.Headers.ContentType = new MediaTypeHeaderValue(request.MimeType);
        }

        form.Add(fileContent, "file", request.FileName);
        form.Add(new StringContent(_model), "model");
        form.Add(new StringContent("text"), "response_format");

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Content = form,
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            using var response = await httpClient.SendAsync(requestMessage, ct);
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "OpenAI transcription returned non-success status {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return null;
            }

            var transcript = ExtractTranscript(body);
            if (string.IsNullOrWhiteSpace(transcript))
            {
                logger.LogWarning("OpenAI transcription response did not include text.");
                return null;
            }

            return transcript.Trim();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while transcribing audio with OpenAI.");
            return null;
        }
    }

    private static string ExtractTranscript(string body)
    {
        var trimmed = body.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return string.Empty;
        }

        if (!trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return trimmed;
        }

        using var document = JsonDocument.Parse(trimmed);
        var root = document.RootElement;

        if (
            root.TryGetProperty("text", out var textElement)
            && textElement.ValueKind == JsonValueKind.String
        )
        {
            return textElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }
}
