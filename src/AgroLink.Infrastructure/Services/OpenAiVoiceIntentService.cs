using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgroLink.Application.Features.VoiceCommands.DTOs;
using AgroLink.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class OpenAiVoiceIntentService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAiVoiceIntentService> logger
) : IVoiceIntentService
{
    private const string SystemPrompt =
        "You are an agricultural assistant for a cattle ranch in Nicaragua. "
        + "Extract a structured intent from a Spanish voice command. "
        + "You will be given the transcription and a JSON roster of animals and lots on this farm. "
        + "Return ONLY a JSON object — no markdown, no explanation. "
        + "Only use IDs that appear verbatim in the roster. "
        + "If you cannot find a confident match, set the ID field to null. "
        + "Supported intents: create_note, move_animal, move_lot, register_newborn. "
        + "If the command does not match a supported intent, return intent \"unknown\" with confidence 0.0. "
        + "JSON fields: intent, confidence, animalId, lotId, targetPaddockId, motherId, sex, newbornEarTag, noteText.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;

    private readonly string _baseUrl =
        configuration["OpenAI:ChatCompletionsBaseUrl"]
        ?? "https://api.openai.com/v1/chat/completions";

    private readonly string _model = configuration["OpenAI:VoiceIntentModel"] ?? "gpt-4o";

    public async Task<string?> ExtractIntentAsync(
        string transcript,
        FarmRosterDto roster,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("OpenAI API key not configured; cannot extract voice intent.");
            return null;
        }

        var rosterJson = JsonSerializer.Serialize(roster, JsonOptions);
        var userMessage = $"Transcription: {transcript}\n\nRoster:\n{rosterJson}";

        var payload = JsonSerializer.Serialize(
            new
            {
                model = _model,
                messages = new object[]
                {
                    new { role = "system", content = SystemPrompt },
                    new { role = "user", content = userMessage },
                },
                response_format = new { type = "json_object" },
                max_tokens = 300,
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
            var body = await response.Content.ReadAsStringAsync(ct);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning(
                    "GPT-4o intent extraction returned {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return null;
            }

            return ExtractContent(body);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error during voice intent extraction.");
            return null;
        }
    }

    private static string? ExtractContent(string body)
    {
        using var doc = JsonDocument.Parse(body);
        var root = doc.RootElement;

        if (root.TryGetProperty("choices", out var choices) && choices.GetArrayLength() > 0)
        {
            var first = choices[0];
            if (
                first.TryGetProperty("message", out var message)
                && message.TryGetProperty("content", out var content)
                && content.ValueKind == JsonValueKind.String
            )
            {
                return content.GetString();
            }
        }

        return null;
    }
}
