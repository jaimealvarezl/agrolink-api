using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class User
{
    public int Id { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MaxLength(100)]
    public string PasswordHash { get; set; } = string.Empty;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "USER"; // ADMIN, USER, WORKER

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<Movement> Movements { get; set; } = new List<Movement>();
    public virtual ICollection<Checklist> Checklists { get; set; } = new List<Checklist>();
}
