using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class Owner
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(20)]
    public string? Phone { get; set; }

    public int? UserId { get; set; }
    public virtual User? User { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<AnimalOwner> AnimalOwners { get; set; } = new List<AnimalOwner>();
    public virtual ICollection<Farm> Farms { get; set; } = new List<Farm>();
}
