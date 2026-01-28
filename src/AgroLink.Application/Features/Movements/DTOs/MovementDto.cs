namespace AgroLink.Application.Features.Movements.DTOs;

public class MovementDto
{
    public required int Id { get; set; }
    public required string EntityType { get; set; }
    public required int EntityId { get; set; }
    public string? EntityName { get; set; }
    public int? FromId { get; set; }
    public string? FromName { get; set; }
    public int? ToId { get; set; }
    public string? ToName { get; set; }
    public required DateTime At { get; set; }
    public string? Reason { get; set; }
    public required int UserId { get; set; }
    public required string UserName { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class CreateMovementDto
{
    public required string EntityType { get; set; }
    public required int EntityId { get; set; }
    public int? FromId { get; set; }
    public int? ToId { get; set; }
    public DateTime At { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
}
