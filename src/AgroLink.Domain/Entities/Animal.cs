using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Entities;

public class Animal
{
    public int Id { get; set; }

    [MaxLength(50)]
    public string? Cuia { get; set; } // Código Único de Identificación Animal - Optional/Nullable

    [Required]
    [MaxLength(50)]
    public string TagVisual { get; set; } = string.Empty;

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? Breed { get; set; }

    [Required]
    [MaxLength(10)]
    public string Sex { get; set; } = string.Empty; // MALE, FEMALE

    public LifeStatus LifeStatus { get; set; } = LifeStatus.Active;
    public ProductionStatus ProductionStatus { get; set; } = ProductionStatus.Calf;
    public HealthStatus HealthStatus { get; set; } = HealthStatus.Healthy;
    public ReproductiveStatus ReproductiveStatus { get; set; } = ReproductiveStatus.NotApplicable;

    public DateTime? BirthDate { get; set; }
    public int LotId { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Lot Lot { get; set; } = null!;
    public virtual Animal? Mother { get; set; }
    public virtual Animal? Father { get; set; }
    public virtual ICollection<Animal> Children { get; set; } = new List<Animal>();
    public virtual ICollection<AnimalOwner> AnimalOwners { get; set; } = new List<AnimalOwner>();
    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();

    public virtual ICollection<ChecklistItem> ChecklistItems { get; set; } =
        new List<ChecklistItem>();

    public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();
}
