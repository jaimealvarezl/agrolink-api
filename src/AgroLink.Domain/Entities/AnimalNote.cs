using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class AnimalNote
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    [Required]
    [MaxLength(2000)]
    public string Content { get; set; } = string.Empty;

    public int UserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Animal Animal { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}
