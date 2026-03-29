using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class OwnerBrand
{
    public int Id { get; set; }

    public int OwnerId { get; set; }
    public virtual Owner Owner { get; set; } = null!;

    [Required]
    [MaxLength(100)]
    public string RegistrationNumber { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? PhotoUrl { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<AnimalBrand> AnimalBrands { get; set; } = new List<AnimalBrand>();
}
