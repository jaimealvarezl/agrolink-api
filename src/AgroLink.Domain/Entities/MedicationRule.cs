using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class MedicationRule
{
    public int Id { get; set; }

    public int MedicationId { get; set; }

    [Required]
    [MaxLength(50)]
    public string Species { get; set; } = "Bovine";

    [MaxLength(500)]
    public string? SymptomTags { get; set; }

    public decimal? WeightMin { get; set; }
    public decimal? WeightMax { get; set; }

    [Required]
    [MaxLength(1000)]
    public string DoseFormula { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string? Contraindications { get; set; }

    public bool Active { get; set; } = true;

    public virtual Medication Medication { get; set; } = null!;
}
