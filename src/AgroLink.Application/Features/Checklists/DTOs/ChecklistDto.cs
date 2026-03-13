using System.ComponentModel.DataAnnotations;

namespace AgroLink.Application.Features.Checklists.DTOs;

public class ChecklistDto
{
    public required int Id { get; set; }

    [Required]
    public required int LotId { get; set; }

    public string? LotName { get; set; }

    [Required]
    public required DateTime Date { get; set; }

    [Required]
    public required int UserId { get; set; }

    [Required]
    public required string UserName { get; set; }

    public string? Notes { get; set; }

    [Required]
    public required List<ChecklistItemDto> Items { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}

public class CreateChecklistDto
{
    [Required]
    public required int LotId { get; set; }

    [Required]
    public DateTime Date { get; set; } = DateTime.UtcNow;

    public string? Notes { get; set; }

    [Required]
    public List<CreateChecklistItemDto> Items { get; set; } = new();
}

public class ChecklistItemDto
{
    public required int Id { get; set; }

    [Required]
    public required int AnimalId { get; set; }

    public string? AnimalCuia { get; set; }
    public string? AnimalName { get; set; }

    public int? AnimalLotId { get; set; }
    public string? AnimalLotName { get; set; }

    [Required]
    public required bool Present { get; set; }

    [Required]
    public required string Condition { get; set; }

    public string? Notes { get; set; }
}

public class CreateChecklistItemDto
{
    [Required]
    public int AnimalId { get; set; }

    [Required]
    public bool Present { get; set; }

    [Required]
    public string Condition { get; set; } = "OK";

    public string? Notes { get; set; }
}
