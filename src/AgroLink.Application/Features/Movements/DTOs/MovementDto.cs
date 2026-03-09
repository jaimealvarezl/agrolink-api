using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Movements.DTOs;

public class MovementDto
{
    public required int Id { get; set; }

    [Required]
    public required int AnimalId { get; set; }

    public string? AnimalName { get; set; }

    public int? FromLotId { get; set; }
    public string? FromLotName { get; set; }

    public int? ToLotId { get; set; }
    public string? ToLotName { get; set; }

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
    public DateTime At { get; set; } = DateTime.UtcNow;

    public string? Reason { get; set; }
}
