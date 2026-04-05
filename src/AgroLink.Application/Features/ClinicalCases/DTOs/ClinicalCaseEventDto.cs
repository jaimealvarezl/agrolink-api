namespace AgroLink.Application.Features.ClinicalCases.DTOs;

public class ClinicalCaseEventDto
{
    public int Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? Transcript { get; init; }
    public double Confidence { get; init; }
    public DateTime CreatedAt { get; init; }
}
