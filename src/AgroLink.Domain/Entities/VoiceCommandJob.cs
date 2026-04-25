using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class VoiceCommandJob
{
    public Guid Id { get; set; }
    public int FarmId { get; set; }
    public int UserId { get; set; }

    [MaxLength(500)]
    public string S3Key { get; set; } = string.Empty;

    [MaxLength(20)]
    public string Status { get; set; } = "pending";

    public string? ResultJson { get; set; }

    [MaxLength(500)]
    public string? ErrorMessage { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }

    public virtual Farm? Farm { get; set; }
}
