using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class Medication
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    public string? TechnicalSheet { get; set; }

    public bool Active { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ICollection<MedicationRule> Rules { get; set; } = new List<MedicationRule>();
    public virtual ICollection<MedicationImage> Images { get; set; } = new List<MedicationImage>();
}
