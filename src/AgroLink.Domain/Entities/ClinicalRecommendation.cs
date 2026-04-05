using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Entities;

public class ClinicalRecommendation
{
    public int Id { get; set; }

    public int ClinicalCaseId { get; set; }

    public RecommendationSource RecommendationSource { get; set; } =
        RecommendationSource.AiExploratory;

    [Required]
    [MaxLength(5000)]
    public string AdviceText { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Disclaimer { get; set; } = string.Empty;

    public string? RawModelResponse { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ClinicalCase ClinicalCase { get; set; } = null!;
}
