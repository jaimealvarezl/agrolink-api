using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class ClinicalAlert
{
    public int Id { get; set; }

    public int ClinicalCaseId { get; set; }

    [Required]
    [MaxLength(50)]
    public string AlertType { get; set; } = string.Empty;

    [Required]
    [MaxLength(2000)]
    public string Message { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "Open";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ClinicalCase ClinicalCase { get; set; } = null!;
}
