using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class MedicationImage
{
    public int Id { get; set; }

    public int MedicationId { get; set; }

    [Required]
    [MaxLength(500)]
    public string ImageUrl { get; set; } = string.Empty;

    [MaxLength(150)]
    public string? Label { get; set; }

    public virtual Medication Medication { get; set; } = null!;
}
