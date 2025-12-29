using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class Farm
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string? Location { get; set; }

    public int OwnerId { get; set; }
    public virtual Owner? Owner { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<Paddock> Paddocks { get; set; } = new List<Paddock>();
}
