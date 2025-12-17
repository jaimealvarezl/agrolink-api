using AgroLink.Application.Features.Photos.DTOs;

namespace AgroLink.Application.Features.Checklists.DTOs;

public class ChecklistDto
{
    public int Id { get; set; }
    public string ScopeType { get; set; } = string.Empty;
    public int ScopeId { get; set; }
    public string? ScopeName { get; set; }
    public DateTime Date { get; set; }
    public int UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public List<ChecklistItemDto> Items { get; set; } = new();
    public List<PhotoDto> Photos { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}

public class CreateChecklistDto
{
    public string ScopeType { get; set; } = string.Empty;
    public int ScopeId { get; set; }
    public DateTime Date { get; set; } = DateTime.UtcNow;
    public string? Notes { get; set; }
    public List<CreateChecklistItemDto> Items { get; set; } = new();
}

public class ChecklistItemDto
{
    public int Id { get; set; }
    public int AnimalId { get; set; }
    public string AnimalTag { get; set; } = string.Empty;
    public string? AnimalName { get; set; }
    public bool Present { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? Notes { get; set; }
}

public class CreateChecklistItemDto
{
    public int AnimalId { get; set; }
    public bool Present { get; set; }
    public string Condition { get; set; } = "OK";
    public string? Notes { get; set; }
}
