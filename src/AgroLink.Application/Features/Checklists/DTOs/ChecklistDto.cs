using AgroLink.Application.Features.Photos.DTOs;

namespace AgroLink.Application.Features.Checklists.DTOs;

public class ChecklistDto
{
    public required int Id { get; set; }
    public required string ScopeType { get; set; }
    public required int ScopeId { get; set; }
    public string? ScopeName { get; set; }
    public required DateTime Date { get; set; }
    public required int UserId { get; set; }
    public required string UserName { get; set; }
    public string? Notes { get; set; }
    public required List<ChecklistItemDto> Items { get; set; }
    public required List<PhotoDto> Photos { get; set; }
    public required DateTime CreatedAt { get; set; }
}

public class CreateChecklistDto
{
    public required string ScopeType { get; set; }
    public required int ScopeId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<CreateChecklistItemDto> Items { get; set; } = new();
}

public class ChecklistItemDto
{
    public required int Id { get; set; }
    public required int AnimalId { get; set; }
    public required string AnimalTag { get; set; }
    public string? AnimalName { get; set; }
    public required bool Present { get; set; }
    public required string Condition { get; set; }
    public string? Notes { get; set; }
}

public class CreateChecklistItemDto
{
    public int AnimalId { get; set; }
    public bool Present { get; set; }
    public string Condition { get; set; } = "OK";
    public string? Notes { get; set; }
}
