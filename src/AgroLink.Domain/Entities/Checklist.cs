using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class Checklist
{
    public int Id { get; set; }

    [Required]
    [MaxLength(20)]
    public string ScopeType { get; set; } = string.Empty; // LOT, PADDOCK

    public int ScopeId { get; set; }

    public DateTime Date { get; set; } = DateTime.UtcNow;

    public int UserId { get; set; }

    [MaxLength(1000)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User User { get; set; } = null!;

    public virtual ICollection<ChecklistItem> ChecklistItems { get; set; } =
        new List<ChecklistItem>();
}
