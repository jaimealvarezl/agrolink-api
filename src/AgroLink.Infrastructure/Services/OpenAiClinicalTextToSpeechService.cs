using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class OpenAiClinicalTextToSpeechService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAiClinicalTextToSpeechService> logger
) : IClinicalTextToSpeechService
{
    private readonly string _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;
    private readonly string _baseUrl =
        configuration["OpenAI:TtsBaseUrl"] ?? "https://api.openai.com/v1/audio/speech";
    private readonly string _model = configuration["OpenAI:TtsModel"] ?? "gpt-4o-mini-tts";
    private readonly string _voice = configuration["OpenAI:TtsVoice"] ?? "alloy";
    private readonly string _format = configuration["OpenAI:TtsFormat"] ?? "mp3";

    public async Task<ClinicalTextToSpeechResult?> SynthesizeAsync(
        ClinicalTextToSpeechRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(request.Text))
        {
            return null;
        }

        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning(
                "OpenAI API key is not configured, voice synthesis cannot be generated."
            );
            return null;
        }

        var selectedFormat = request.Format ?? _format;
        var payload = JsonSerializer.Serialize(
            new
            {
                model = _model,
                voice = request.Voice ?? _voice,
                input = request.Text,
                response_format = selectedFormat,
            }
        );

        using var requestMessage = new HttpRequestMessage(HttpMethod.Post, _baseUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        try
        {
            using var response = await httpClient.SendAsync(requestMessage, ct);

            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                logger.LogWarning(
                    "OpenAI text-to-speech returned non-success status {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return new ClinicalTextToSpeechResult { Success = false, ProviderResponse = body };
            }

            var content = await response.Content.ReadAsByteArrayAsync(ct);
            if (content.Length == 0)
            {
                logger.LogWarning("OpenAI text-to-speech returned an empty audio payload.");
                return new ClinicalTextToSpeechResult
                {
                    Success = false,
                    ProviderResponse = "OpenAI returned an empty audio payload.",
                };
            }

            var extension = GetFileExtension(selectedFormat);
            var mimeType = GetMimeType(selectedFormat);
            return new ClinicalTextToSpeechResult
            {
                Success = true,
                AudioContent = content,
                FileName = $"clinical-recommendation.{extension}",
                MimeType = mimeType,
                ProviderResponse = "ok",
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error while generating speech with OpenAI.");
            return new ClinicalTextToSpeechResult
            {
                Success = false,
                ProviderResponse = ex.Message,
            };
        }
    }

    private static string GetFileExtension(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "wav" => "wav",
            "aac" => "aac",
            "flac" => "flac",
            "opus" => "ogg",
            _ => "mp3",
        };
    }

    private static string GetMimeType(string format)
    {
        return format.ToLowerInvariant() switch
        {
            "wav" => "audio/wav",
            "aac" => "audio/aac",
            "flac" => "audio/flac",
            "opus" => "audio/ogg",
            _ => "audio/mpeg",
        };
    }
}
