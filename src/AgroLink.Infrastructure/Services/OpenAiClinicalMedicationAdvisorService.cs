using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AgroLink.Infrastructure.Services;

public class OpenAiClinicalMedicationAdvisorService(
    HttpClient httpClient,
    IConfiguration configuration,
    ILogger<OpenAiClinicalMedicationAdvisorService> logger
) : IClinicalMedicationAdvisorService
{
    private readonly string _apiKey = configuration["OpenAI:ApiKey"] ?? string.Empty;

    private readonly string _baseUrl =
        configuration["OpenAI:BaseUrl"] ?? "https://api.openai.com/v1/responses";

    private readonly string _model = configuration["OpenAI:Model"] ?? "gpt-4o-mini";

    public async Task<ClinicalMedicationAdviceResult> GetAdviceAsync(
        ClinicalMedicationAdviceRequest request,
        CancellationToken ct = default
    )
    {
        if (string.IsNullOrWhiteSpace(_apiKey))
        {
            return BuildFallbackAdvice(request, "OpenAI API key is not configured.");
        }

        var systemPrompt =
            "Actua como un doctor veterinario expero en granjas y ganado bovino en Nicaragua. "
            + "Responde solo en espanol claro y breve. "
            + "No uses markdown, hashtags, ni formato con asteriscos. "
            + "No uses listas con guiones ni listas numeradas. "
            + "Da un contexto muy breve de la enfermedad "
            + "No incluyas advertencias ni descargos. "
            + "Devuelve exactamente estas 3 secciones y en este orden: "
            + "\"Sintomas a monitorear:\", "
            + "\"Tratamiento recomendado:\", "
            + "\"Consejos:\". "
            + "En tratamiento recomendado, incluye opciones practicas con dosis orientativa y donde conseguirlas en Nicaragua cuando aplique.";

        var userPrompt =
            $"Granja: {request.FarmName}\n"
            + $"Animal: {request.AnimalReference}\n"
            + $"Arete/CUIA: {request.EarTag}\n"
            + $"Sintomas: {request.SymptomsSummary}\n"
            + $"Transcript: {request.TranscriptText}";

        var payload = JsonSerializer.Serialize(
            new
            {
                model = _model,
                input = new object[]
                {
                    new
                    {
                        role = "system",
                        content = new object[] { new { type = "input_text", text = systemPrompt } },
                    },
                    new
                    {
                        role = "user",
                        content = new object[] { new { type = "input_text", text = userPrompt } },
                    },
                },
                max_output_tokens = 700,
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
                    "OpenAI medication advisor returned non-success status {StatusCode}. Body: {Body}",
                    response.StatusCode,
                    body
                );
                return BuildFallbackAdvice(
                    request,
                    $"OpenAI request failed with status {(int)response.StatusCode}."
                );
            }

            var outputText = ExtractOutputText(body);
            if (string.IsNullOrWhiteSpace(outputText))
            {
                return BuildFallbackAdvice(request, "OpenAI output text was empty.");
            }

            return new ClinicalMedicationAdviceResult
            {
                AdviceText = outputText,
                Disclaimer = string.Empty,
                RiskLevel = InferRiskLevel(outputText),
                ConfidenceScore = 0.65,
                RawModelResponse = body,
            };
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while requesting medication advice from OpenAI.");
            return BuildFallbackAdvice(request, "Unexpected error while contacting OpenAI.");
        }
    }

    private static string ExtractOutputText(string body)
    {
        using var document = JsonDocument.Parse(body);
        var root = document.RootElement;

        if (
            root.TryGetProperty("output_text", out var outputTextElement)
            && outputTextElement.ValueKind == JsonValueKind.String
        )
        {
            return outputTextElement.GetString() ?? string.Empty;
        }

        if (
            root.TryGetProperty("output", out var outputElement)
            && outputElement.ValueKind == JsonValueKind.Array
        )
        {
            foreach (var outputItem in outputElement.EnumerateArray())
            {
                if (
                    outputItem.TryGetProperty("content", out var contentElement)
                    && contentElement.ValueKind == JsonValueKind.Array
                )
                {
                    foreach (var contentItem in contentElement.EnumerateArray())
                    {
                        if (
                            contentItem.TryGetProperty("text", out var textElement)
                            && textElement.ValueKind == JsonValueKind.String
                        )
                        {
                            var value = textElement.GetString();
                            if (!string.IsNullOrWhiteSpace(value))
                            {
                                return value;
                            }
                        }
                    }
                }
            }
        }

        return string.Empty;
    }

    private static ClinicalRiskLevel InferRiskLevel(string adviceText)
    {
        var normalized = adviceText.ToLowerInvariant();
        if (
            normalized.Contains("urgencia")
            || normalized.Contains("emergencia")
            || normalized.Contains("severo")
        )
        {
            return ClinicalRiskLevel.High;
        }

        if (normalized.Contains("monitorear") || normalized.Contains("vigilar"))
        {
            return ClinicalRiskLevel.Medium;
        }

        return ClinicalRiskLevel.Low;
    }

    private static ClinicalMedicationAdviceResult BuildFallbackAdvice(
        ClinicalMedicationAdviceRequest request,
        string reason
    )
    {
        var text =
            "Sintomas a monitorear:\n"
            + $"- {request.SymptomsSummary}\n"
            + "- Decaimiento, falta de apetito y cambios respiratorios o digestivos.\n\n"
            + "Tratamiento recomendado:\n"
            + "- Consulta en veterinarias agropecuarias y distribuidores veterinarios de Nicaragua por opciones para el cuadro reportado.\n"
            + "- Solicita dosis orientativa por peso del animal y confirma via de aplicacion segun etiqueta del producto.\n\n"
            + "Consejos:\n"
            + "- Registra temperatura y evolucion cada 12 horas.\n"
            + "- Mantener agua limpia, sombra y aislamiento del animal sintomatico.\n"
            + $"- Nota tecnica: no se pudo consultar IA en linea ({reason}).";

        return new ClinicalMedicationAdviceResult
        {
            AdviceText = text,
            Disclaimer = string.Empty,
            RiskLevel = ClinicalRiskLevel.Medium,
            ConfidenceScore = 0.2,
            RawModelResponse = "{}",
        };
    }
}
