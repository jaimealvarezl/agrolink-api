using System.Text.RegularExpressions;
using AgroLink.Application.Features.ClinicalCases.Models;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Common.Services;

public class HeuristicClinicalExtractionService : IClinicalExtractionService
{
    private static readonly Regex FarmRegex = new(
        "(?:granja|finca|farm)\\s*[:=-]\\s*(?<value>[^,;\\n]+?)\\s*(?=(?:animal|vaca|toro|novillo|ternera|res|arete|ear\\s*tag|tag|cuia|sintomas?)\\s*[:=-]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex AnimalRegex = new(
        "(?:animal|vaca|toro|novillo|ternera|res)\\s*[:=-]\\s*(?<value>[^,;\\n]+?)\\s*(?=(?:arete|ear\\s*tag|tag|cuia|sintomas?)\\s*[:=-]|$)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    private static readonly Regex EarTagRegex = new(
        "(?:arete|ear\\s*tag|tag|cuia)\\s*[:=-]\\s*(?<value>[A-Za-z0-9-]+)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled
    );

    public Task<ClinicalExtractionResult> ExtractAsync(
        string messageText,
        CancellationToken ct = default
    )
    {
        var sanitized = (messageText ?? string.Empty).Trim();

        var farmReference = MatchValue(FarmRegex, sanitized);
        var animalReference = MatchValue(AnimalRegex, sanitized);
        var earTag = MatchValue(EarTagRegex, sanitized);

        var intent = GetIntent(sanitized);

        var confidenceScore = 0.35;
        if (!string.IsNullOrWhiteSpace(farmReference))
        {
            confidenceScore += 0.2;
        }

        if (!string.IsNullOrWhiteSpace(animalReference))
        {
            confidenceScore += 0.2;
        }

        if (!string.IsNullOrWhiteSpace(earTag))
        {
            confidenceScore += 0.25;
        }

        if (confidenceScore > 1)
        {
            confidenceScore = 1;
        }

        var confidenceLevel = confidenceScore switch
        {
            >= 0.75 => ExtractionConfidenceLevel.High,
            >= 0.5 => ExtractionConfidenceLevel.Medium,
            _ => ExtractionConfidenceLevel.Low,
        };

        return Task.FromResult(
            new ClinicalExtractionResult
            {
                Intent = intent,
                FarmReference = farmReference,
                AnimalReference = animalReference,
                EarTag = earTag,
                SymptomsSummary = sanitized,
                ConfidenceLevel = confidenceLevel,
                ConfidenceScore = confidenceScore,
            }
        );
    }

    private static string? MatchValue(Regex regex, string input)
    {
        var match = regex.Match(input);
        if (!match.Success)
        {
            return null;
        }

        var value = match.Groups["value"].Value.Trim();
        return string.IsNullOrWhiteSpace(value) ? null : value;
    }

    private static ClinicalMessageIntent GetIntent(string message)
    {
        var normalized = message.ToLowerInvariant();

        if (
            normalized.Contains("estado")
            || normalized.Contains("status")
            || normalized.Contains("reporte")
            || normalized.Contains("como esta")
            || normalized.Contains("como va")
        )
        {
            return ClinicalMessageIntent.AnimalStatusRequest;
        }

        if (normalized is "si" or "yes" or "no")
        {
            return ClinicalMessageIntent.ConfirmationReply;
        }

        return ClinicalMessageIntent.NewCaseReport;
    }
}
