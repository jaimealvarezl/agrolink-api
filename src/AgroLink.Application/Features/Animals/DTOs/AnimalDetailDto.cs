using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalDetailDto
{
    public required int Id { get; set; }
    public required string TagVisual { get; set; }
    public string? Cuia { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public required Sex Sex { get; set; }
    public required DateTime BirthDate { get; set; }
    public int AgeInMonths { get; set; }
    
    // Location
    public required int LotId { get; set; }
    public required string LotName { get; set; }
    
    // Status
    public required LifeStatus LifeStatus { get; set; }
    public required ProductionStatus ProductionStatus { get; set; }
    public required HealthStatus HealthStatus { get; set; }
    public required ReproductiveStatus ReproductiveStatus { get; set; }

    // Genealogy
    public string? MotherName { get; set; }
    public string? FatherName { get; set; }

    // Ownership
    public required List<AnimalOwnerDto> Owners { get; set; } = new();
    
    // Photos
    public string? PrimaryPhotoUrl { get; set; }
}
