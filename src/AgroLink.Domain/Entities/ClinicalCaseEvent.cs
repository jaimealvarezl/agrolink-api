using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class ClinicalCaseEvent
{
    public int Id { get; set; }

    public int ClinicalCaseId { get; set; }

    [Required]
    [MaxLength(50)]
    public string EventType { get; set; } = string.Empty;

    [Required]
    public string RawPayloadJson { get; set; } = "{}";

    public string? Transcript { get; set; }
    public string? StructuredDataJson { get; set; }

    public double Confidence { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ClinicalCase ClinicalCase { get; set; } = null!;
}
