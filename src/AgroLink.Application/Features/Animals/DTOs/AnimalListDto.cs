using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalListDto
{
    public required int Id { get; set; }
    public string? TagVisual { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? PhotoUrl { get; set; }

    [Required]
    public required string LotName { get; set; }

    public Sex Sex { get; set; }
    public DateTime? BirthDate { get; set; }

    // Status Flags
    public bool IsSick { get; set; }
    public bool IsPregnant { get; set; }
    public bool IsMissing { get; set; }
}
