using AgroLink.Application.Features.Movements.DTOs;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalTimelineItemDto
{
    /// <summary>"note" | "movement" | "checklist"</summary>
    public string Type { get; set; } = string.Empty;

    public DateTime OccurredAt { get; set; }

    public AnimalNoteDto? Note { get; set; }
    public MovementDto? Movement { get; set; }
    public AnimalChecklistTimelineDto? ChecklistItem { get; set; }
}

public class AnimalChecklistTimelineDto
{
    public int ChecklistId { get; set; }
    public int ChecklistItemId { get; set; }
    public DateTime Date { get; set; }
    public int LotId { get; set; }
    public string? LotName { get; set; }
    public bool Present { get; set; }
    public string Condition { get; set; } = string.Empty;
    public string? Notes { get; set; }
}
