using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalDetailDto
{
    public required int Id { get; set; }
    public string? TagVisual { get; set; }
    public string? Cuia { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Color { get; set; }
    public string? Breed { get; set; }

    [Required]
    public required Sex Sex { get; set; }

    [Required]
    public required DateTime BirthDate { get; set; }

    public int AgeInMonths { get; set; }

    // Location
    public required int LotId { get; set; }

    [Required]
    public required string LotName { get; set; }

    // Status
    [Required]
    public required LifeStatus LifeStatus { get; set; }

    [Required]
    public required ProductionStatus ProductionStatus { get; set; }

    [Required]
    public required HealthStatus HealthStatus { get; set; }

    [Required]
    public required ReproductiveStatus ReproductiveStatus { get; set; }

    // Genealogy
    public string? MotherName { get; set; }
    public string? FatherName { get; set; }

    // Ownership
    [Required]
    public required List<AnimalOwnerDto> Owners { get; set; } = new();

    // Photos
    public string? PrimaryPhotoUrl { get; set; }
}
