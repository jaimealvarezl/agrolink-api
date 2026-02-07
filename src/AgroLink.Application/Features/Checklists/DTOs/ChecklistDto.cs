using System.ComponentModel.DataAnnotations;
using AgroLink.Application.Features.Photos.DTOs;

namespace AgroLink.Application.Features.Checklists.DTOs;

public class ChecklistDto
{
    public required int Id { get; set; }

    [Required]
    public required string ScopeType { get; set; }

    [Required]
    public required int ScopeId { get; set; }
    public string? ScopeName { get; set; }

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
    public required List<PhotoDto> Photos { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }
}

public class CreateChecklistDto
{
    [Required]
    public required string ScopeType { get; set; }

    [Required]
    public required int ScopeId { get; set; }

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
