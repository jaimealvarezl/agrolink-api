using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class TelegramOutboundMessage
{
    public int Id { get; set; }

    public int? ClinicalCaseId { get; set; }

    public long ChatId { get; set; }

    [Required]
    [MaxLength(50)]
    public string MessageType { get; set; } = "Text";

    [Required]
    public string PayloadJson { get; set; } = "{}";

    [Required]
    [MaxLength(200)]
    public string IdempotencyKey { get; set; } = string.Empty;

    public long? TelegramMessageId { get; set; }

    [Required]
    [MaxLength(50)]
    public string DeliveryStatus { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual ClinicalCase? ClinicalCase { get; set; }
}
