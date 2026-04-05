using System.ComponentModel.DataAnnotations;

namespace AgroLink.Domain.Entities;

public class TelegramInboundEventLog
{
    public int Id { get; set; }

    public long TelegramUpdateId { get; set; }

    public long? ChatId { get; set; }
    public long? MessageId { get; set; }

    [Required]
    public string RawPayloadJson { get; set; } = "{}";

    public bool Processed { get; set; }

    [Required]
    [MaxLength(50)]
    public string ProcessingStatus { get; set; } = "Received";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ProcessedAt { get; set; }
}
