namespace AgroLink.Core.DTOs;

public class MovementDto
{
    public int Id { get; set; }
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public string? EntityName { get; set; }
    public int? FromId { get; set; }
    public string? FromName { get; set; }
    public int? ToId { get; set; }
    public string? ToName { get; set; }
    public DateTime At { get; set; }
    public string? Reason { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class CreateMovementDto
{
    public string EntityType { get; set; } = string.Empty;
    public int EntityId { get; set; }
    public int? FromId { get; set; }
    public int? ToId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
