using System.ComponentModel.DataAnnotations;

namespace AgroLink.Core.Entities;

public class AnimalOwner
{
    public int AnimalId { get; set; }
    public int OwnerId { get; set; }
    
    [Range(0.01, 100.00)]
    public decimal SharePercent { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public virtual Animal Animal { get; set; } = null!;
    public virtual Owner Owner { get; set; } = null!;
}