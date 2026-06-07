using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class DeviceToken
{
    public int Id { get; set; }

    public int UserId { get; set; }

    [Required]
    [MaxLength(512)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MaxLength(10)]
    public string Platform { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastSeenAt { get; set; } = DateTime.UtcNow;

    public virtual User User { get; set; } = null!;
}
