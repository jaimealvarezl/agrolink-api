using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
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
    private const string SystemPromptTemplate =
        "You are an agricultural assistant for a cattle ranch in Nicaragua. "
        + "Extract a structured intent from a Spanish voice command. "
        + "Return ONLY a JSON object — no markdown, no explanation. "
        + "Do NOT resolve entity names to IDs. Instead, return the exact spoken text for each entity reference. "
        + "Supported intents: create_animal, create_note, move_animal, move_lot, register_newborn. "
        + "If the command does not match a supported intent, return intent \"unknown\" with confidence 0.0. "
        + "For move_animal: set animalMention (spoken animal reference), lotMention (spoken lot name). "
        + "For move_lot: set lotMention (spoken lot name), targetPaddockMention (spoken paddock name). "
        + "For create_note: set animalMention (spoken animal reference), noteText. "
        + "For register_newborn: set motherMention (spoken mother reference), sex, color (coat color if mentioned), birthDate (ISO 8601 — today is {today}). "
        + "For create_animal: set animalName (name given), earTag (tag or CUIA number spoken), sex (vaca/ternera→female, toro/ternero→male), color, lotMention (spoken lot name), ownerNames (array), motherId (resolved from motherMention if calf of known mother), birthDate (ISO 8601 — today is {today}). "
        + "JSON fields: intent, confidence, animalMention, lotMention, targetPaddockMention, motherMention, sex, noteText, animalName, earTag, color, birthDate, ownerNames.";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;

    private readonly string _baseUrl =
        configuration["OpenAI:ChatCompletionsBaseUrl"]
        ?? "https://api.openai.com/v1/chat/completions";

    private readonly string _model = configuration["OpenAI:VoiceIntentModel"] ?? "gpt-4o";

    public async Task<string?> ExtractIntentAsync(string transcript, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("OpenAI API key not configured; cannot extract voice intent.");
            return null;
        }

        var today = DateTime.UtcNow.ToString("yyyy-MM-dd");
        var systemPrompt = SystemPromptTemplate.Replace("{today}", today);

        var payload = JsonSerializer.Serialize(
            new
            {
                model = _model,
                messages = new object[]
                {
                    new { role = "system", content = systemPrompt },
                    new { role = "user", content = $"Transcription: {transcript}" },
                },
                response_format = new { type = "json_object" },
                max_tokens = 250,
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
