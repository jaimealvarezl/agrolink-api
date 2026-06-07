using AgroLink.Domain.Enums;

namespace AgroLink.Domain.Entities;

public class SentNotification
{
    public int Id { get; set; }

    public int AnimalId { get; set; }

    public NotificationType NotificationType { get; set; }

    public DateOnly ExpectedDueDate { get; set; }

    public DateTime SentAt { get; set; } = DateTime.UtcNow;

    public virtual Animal Animal { get; set; } = null!;
}
