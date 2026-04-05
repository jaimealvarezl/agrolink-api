namespace AgroLink.Application.Features.ClinicalCases.Models;

public record ClinicalTextToSpeechRequest(string Text, string? Voice = null, string? Format = null);
