using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Movements.DTOs;

public class MovementDto
{
    public required int Id { get; set; }

    [Required]
    public required string EntityType { get; set; }

    [Required]
    public required int EntityId { get; set; }

    public string? EntityName { get; set; }
    public int? FromId { get; set; }
    public string? FromName { get; set; }
    public int? ToId { get; set; }
    public string? ToName { get; set; }

    [Required]
    public required DateTime At { get; set; }

    public string? Reason { get; set; }

    [Required]
    public required int UserId { get; set; }

    [Required]
    public required string UserName { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}

public class CreateMovementDto
{
    [Required]
    public List<int> AnimalIds { get; set; } = new();

    [Required]
    public int ToLotId { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }
}
