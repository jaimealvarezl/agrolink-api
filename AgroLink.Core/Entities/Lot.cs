using System.ComponentModel.DataAnnotations;

namespace AgroLink.Core.Entities;

public class Lot
{
    public int Id { get; set; }
    
    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;
    
    public int PaddockId { get; set; }
    
    [Required]
    [MaxLength(50)]
    public string Status { get; set; } = "ACTIVE"; // ACTIVE, INACTIVE, MAINTENANCE
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public virtual Paddock Paddock { get; set; } = null!;
    public virtual ICollection<Animal> Animals { get; set; } = new List<Animal>();
    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();
}