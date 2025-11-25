using System.ComponentModel.DataAnnotations;

namespace AgroLink.Core.Entities;

public class Animal
{
    public int Id { get; set; }

    [Required]
    [MaxLength(50)]
    public string Tag { get; set; } = string.Empty; // Unique identifier tag

    [MaxLength(200)]
    public string? Name { get; set; }

    [MaxLength(100)]
    public string? Color { get; set; }

    [MaxLength(100)]
    public string? Breed { get; set; }

    [Required]
    [MaxLength(10)]
    public string Sex { get; set; } = string.Empty; // MALE, FEMALE

    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, SOLD, DEAD, MISSING

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
