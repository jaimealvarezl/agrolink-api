using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Constants;

namespace AgroLink.Domain.Entities;

public class FarmMember
{
    public int Id { get; set; }

    [Required]
    public int FarmId { get; set; }
    public virtual Farm Farm { get; set; } = null!;

    [Required]
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;

    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = FarmMemberRoles.Viewer;

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}
