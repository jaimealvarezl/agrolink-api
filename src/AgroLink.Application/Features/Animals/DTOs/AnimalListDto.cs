namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalListDto
{
    public required int Id { get; set; }
    public string? TagVisual { get; set; }
    public required string Name { get; set; }
    public string? PhotoUrl { get; set; }
    public required string LotName { get; set; }

    // Status Flags
    public bool IsSick { get; set; }
    public bool IsPregnant { get; set; }
    public bool IsMissing { get; set; }
}
