namespace AgroLink.Application.Features.Movements.DTOs;

public class MovementDto
{
    public required int Id { get; set; }
    public required int AnimalId { get; set; }
    public required string AnimalTag { get; set; }
    public required int FromLotId { get; set; }
    public required string FromLotName { get; set; }
    public required int ToLotId { get; set; }
    public required string ToLotName { get; set; }
    public required DateTime MovementDate { get; set; }
    public string? Reason { get; set; }
    public required string CreatedBy { get; set; }
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
