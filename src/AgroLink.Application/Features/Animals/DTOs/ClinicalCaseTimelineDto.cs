using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class ClinicalCaseTimelineDto
{
    public int Id { get; init; }
    public ClinicalCaseState State { get; init; }
    public ClinicalRiskLevel RiskLevel { get; init; }
    public DateTime OpenedAt { get; init; }
    public DateTime? ClosedAt { get; init; }
}
