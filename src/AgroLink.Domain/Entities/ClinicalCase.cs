using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Entities;

public class ClinicalCase
{
    public int Id { get; set; }

    public int FarmId { get; set; }
    public int? AnimalId { get; set; }

    [MaxLength(50)]
    public string? EarTag { get; set; }

    [MaxLength(200)]
    public string? FarmReferenceText { get; set; }

    [MaxLength(200)]
    public string? AnimalReferenceText { get; set; }

    public ClinicalCaseState State { get; set; } = ClinicalCaseState.NewCase;
    public ClinicalRiskLevel RiskLevel { get; set; } = ClinicalRiskLevel.Low;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ClosedAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    public virtual Farm Farm { get; set; } = null!;
    public virtual Animal? Animal { get; set; }
    public virtual ICollection<ClinicalCaseEvent> Events { get; set; } =
        new List<ClinicalCaseEvent>();

    public virtual ICollection<ClinicalRecommendation> Recommendations { get; set; } =
        new List<ClinicalRecommendation>();

    public virtual ICollection<ClinicalAlert> Alerts { get; set; } = new List<ClinicalAlert>();
}
