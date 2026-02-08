using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Lots.DTOs;

public class LotDto
{
    public required int Id { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required int PaddockId { get; set; }

    [Required]
    public required int FarmId { get; set; }

    [Required]
    public required string PaddockName { get; set; }

    [Required]
    public required string Status { get; set; }

    [Required]
    public required int AnimalCount { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}

public class CreateLotDto
{
    [Required]
    public required string Name { get; set; }

    [Required]
    public required int PaddockId { get; set; }

    public string? Status { get; set; }
}

public class UpdateLotDto
{
    public string? Name { get; set; }
    public int? PaddockId { get; set; }
    public string? Status { get; set; }
}
