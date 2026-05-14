using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using AgroLink.Application.Features.Animals.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class OpenAiAnimalHealthAnalysisService(
    HttpClient httpClient,
    IStorageService storageService,
    IConfiguration configuration,
    ILogger<OpenAiAnimalHealthAnalysisService> logger
) : IAnimalHealthAnalysisService
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    private readonly string _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;

    private readonly string _chatBaseUrl =
        configuration["OpenAI:ChatBaseUrl"] ?? "https://api.openai.com/v1/chat/completions";

    private readonly string _model = configuration["OpenAI:VisionModel"] ?? "gpt-4o";

    public async Task<AnimalHealthAnalysisResult> AnalyzeAsync(
        AnimalHealthAnalysisRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            logger.LogWarning("OpenAI API key is not configured; cannot perform health analysis.");
            return Rejected("OpenAI API key is not configured.");
        }

        var bioPacket = BuildBioPacket(request);

        // Fresh presigned URL with >5 min expiry as required by the spec.
        var presignedUrl = storageService.GetPresignedUrl(
            request.PhotoStorageKey,
            TimeSpan.FromMinutes(10)
        );

        try
        {
            return await SendAsync(bioPacket, presignedUrl, request.PhotoContentType, ct);
        }
        catch (Exception ex)
        {
            // URL may be unreachable from OpenAI servers (GCS network boundary).
            // Fall back to base64 encoding to eliminate the dependency on URL accessibility.
            logger.LogWarning(ex, "GPT-4o vision URL call failed; retrying with base64 fallback.");
        }

        var bytes = await storageService.GetFileBytesAsync(request.PhotoStorageKey, ct);
        if (bytes == null || bytes.Length == 0)
        {
            return Rejected("No se pudo obtener la imagen del animal para análisis.");
        }

        var dataUrl = $"data:{request.PhotoContentType};base64,{Convert.ToBase64String(bytes)}";
        return await SendAsync(bioPacket, dataUrl, request.PhotoContentType, ct);
    }

    private async Task<AnimalHealthAnalysisResult> SendAsync(
        string bioPacket,
        string imageUrl,
        string contentType,
        CancellationToken ct
    )
    {
        var userText =
            $"Analiza este animal: {bioPacket}. "
            + "Retorna JSON estricto con estos campos exactos: "
            + "{ bodyConditionScore, hasAlert, alertDescription, photoRejected, rejectionReason }";

        var payload = JsonSerializer.Serialize(
            new
            {
                model = _model,
                messages = new object[]
                {
                    new { role = "system", content = "Eres un veterinario experto en bovinos." },
                    new
                    {
                        role = "user",
                        content = new object[]
                        {
                            new { type = "text", text = userText },
                            new { type = "image_url", image_url = new { url = imageUrl } },
                        },
                    },
                },
                max_tokens = 500,
                response_format = new { type = "json_object" },
            },
            JsonOptions
        );

        using var request = new HttpRequestMessage(HttpMethod.Post, _chatBaseUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json"),
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        using var response = await httpClient.SendAsync(request, ct);
        var body = await response.Content.ReadAsStringAsync(ct);

        if (!response.IsSuccessStatusCode)
        {
            logger.LogWarning(
                "GPT-4o vision returned {StatusCode}. Body: {Body}",
                response.StatusCode,
                body
            );
            throw new HttpRequestException(
                $"OpenAI vision request failed with status {(int)response.StatusCode}."
            );
        }

        return ParseResponse(body);
    }

    private AnimalHealthAnalysisResult ParseResponse(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            var content =
                doc.RootElement.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString()
                ?? "{}";

            using var aiDoc = JsonDocument.Parse(content);
            var ai = aiDoc.RootElement;

            return new AnimalHealthAnalysisResult
            {
                BodyConditionScore =
                    ai.TryGetProperty("bodyConditionScore", out var bcs)
                    && bcs.ValueKind == JsonValueKind.Number
                        ? bcs.GetDouble()
                        : 0,
                HasAlert =
                    ai.TryGetProperty("hasAlert", out var ha)
                    && ha.ValueKind is JsonValueKind.True or JsonValueKind.False
                    && ha.GetBoolean(),
                AlertDescription =
                    ai.TryGetProperty("alertDescription", out var ad)
                    && ad.ValueKind == JsonValueKind.String
                        ? ad.GetString()
                        : null,
                PhotoRejected =
                    ai.TryGetProperty("photoRejected", out var pr)
                    && pr.ValueKind is JsonValueKind.True or JsonValueKind.False
                    && pr.GetBoolean(),
                RejectionReason =
                    ai.TryGetProperty("rejectionReason", out var rr)
                    && rr.ValueKind == JsonValueKind.String
                        ? rr.GetString()
                        : null,
                RawAiResponse = body,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to parse GPT-4o vision response. Body: {Body}", body);
            return Rejected("No se pudo interpretar la respuesta de la IA.");
        }
    }

    private static string BuildBioPacket(AnimalHealthAnalysisRequest request)
    {
        var sex = request.Sex == Sex.Female ? "hembra" : "macho";
        var breed = string.IsNullOrWhiteSpace(request.Breed)
            ? "raza no especificada"
            : request.Breed;
        return $"{sex} bovino, {breed}, {DescribeAge(request.BirthDate)}, "
            + $"estado: {DescribeProductionStatus(request.ProductionStatus)}, "
            + $"reproducción: {DescribeReproductiveStatus(request.ReproductiveStatus)}";
    }

    private static string DescribeAge(DateTime birthDate)
    {
        var now = DateTime.UtcNow;
        var months = (now.Year - birthDate.Year) * 12 + now.Month - birthDate.Month;
        if (now.Day < birthDate.Day)
        {
            months--;
        }

        if (months <= 0)
        {
            return "recién nacido";
        }

        if (months < 12)
        {
            return $"{months} mes{(months > 1 ? "es" : "")} de edad";
        }

        var years = months / 12;
        var rem = months % 12;
        var yearStr = $"{years} año{(years > 1 ? "s" : "")}";
        return rem > 0
            ? $"{yearStr} y {rem} mes{(rem > 1 ? "es" : "")} de edad"
            : $"{yearStr} de edad";
    }

    private static string DescribeProductionStatus(ProductionStatus status)
    {
        return status switch
        {
            ProductionStatus.Calf => "ternero/a",
            ProductionStatus.Heifer => "vaquilla",
            ProductionStatus.Milking => "lactante (ordeño)",
            ProductionStatus.Dry => "seca",
            ProductionStatus.Bull => "semental",
            ProductionStatus.Steer => "novillo/engorde",
            _ => status.ToString(),
        };
    }

    private static string DescribeReproductiveStatus(ReproductiveStatus status)
    {
        return status switch
        {
            ReproductiveStatus.NotApplicable => "no aplica",
            ReproductiveStatus.Open => "vacía",
            ReproductiveStatus.Pregnant => "preñada",
            _ => status.ToString(),
        };
    }

    private static AnimalHealthAnalysisResult Rejected(string reason)
    {
        return new AnimalHealthAnalysisResult
        {
            PhotoRejected = true,
            RejectionReason = reason,
            RawAiResponse = "{}",
        };
    }
}
