using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class ChecklistItem
{
    public int Id { get; set; }

    public int ChecklistId { get; set; }
    public int AnimalId { get; set; }

    public bool Present { get; set; }

    [Required]
    [MaxLength(10)]
    public string Condition { get; set; } = "OK"; // OK, OBS (Observation), URG (Urgent)

    [MaxLength(500)]
    public string? Notes { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Checklist Checklist { get; set; } = null!;
    public virtual Animal Animal { get; set; } = null!;
}
