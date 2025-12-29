using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class FarmMember
{
    public int Id { get; set; }

    public int FarmId { get; set; }
    public virtual Farm Farm { get; set; } = null!;

    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Viewer"; // Owner, Admin, Editor, Viewer

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
