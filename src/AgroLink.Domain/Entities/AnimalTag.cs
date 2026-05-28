namespace AgroLink.Domain.Entities;

public class AnimalTag
{
    public int AnimalId { get; set; }
    public int TagId { get; set; }
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    public int AddedByUserId { get; set; }

    public virtual Animal Animal { get; set; } = null!;
    public virtual Tag Tag { get; set; } = null!;
}
