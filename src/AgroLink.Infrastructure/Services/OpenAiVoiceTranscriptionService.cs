using System.Net.Http.Headers;
using System.Text.Json;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class OpenAiVoiceTranscriptionService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAiVoiceTranscriptionService> logger
) : IVoiceTranscriptionService
{
    private readonly string _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;

    private readonly string _baseUrl =
        configuration["OpenAI:TranscriptionBaseUrl"]
        ?? "https://api.openai.com/v1/audio/transcriptions";

    private readonly string _model = configuration["OpenAI:TranscriptionModel"] ?? "whisper-1";

    public async Task<string?> TranscribeAsync(
        byte[] audioContent,
        string fileName,
        string? mimeType,
        string language,
        CancellationToken ct = default
    )
    {
        if (audioContent.Length == 0)
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("OpenAI API key not configured; cannot transcribe voice audio.");
            return null;
        }

        using var form = new MultipartFormDataContent();
        using var fileContent = new ByteArrayContent(audioContent);

        // Do not set Content-Type on the part — Whisper detects format from the filename extension.
        // Setting audio/mp4 causes "could not be decoded" even for valid .m4a files.
        form.Add(fileContent, "file", fileName);
        form.Add(new StringContent(_model), "model");
        form.Add(new StringContent("text"), "response_format");
        form.Add(new StringContent(language), "language");

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
                    "Whisper returned {StatusCode} for voice transcription. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return null;
            }

            return ExtractText(body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during voice audio transcription.");
            return null;
        }
    }

    private static string? ExtractText(string body)
    {
        var trimmed = body.Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return null;
        }

        if (!trimmed.StartsWith("{", StringComparison.Ordinal))
        {
            return trimmed;
        }

        using var doc = JsonDocument.Parse(trimmed);
        if (
            doc.RootElement.TryGetProperty("text", out var el)
            && el.ValueKind == JsonValueKind.String
        )
        {
            return el.GetString();
        }

        return null;
    }
}
