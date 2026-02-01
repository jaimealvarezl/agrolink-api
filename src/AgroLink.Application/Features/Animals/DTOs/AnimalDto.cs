using AgroLink.Application.Features.Photos.DTOs;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalDto
{
    public required int Id { get; set; }
    public string? Cuia { get; set; }
    public required string TagVisual { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public required string Sex { get; set; }

    public required LifeStatus LifeStatus { get; set; }
    public required ProductionStatus ProductionStatus { get; set; }
    public required HealthStatus HealthStatus { get; set; }
    public required ReproductiveStatus ReproductiveStatus { get; set; }

    public required DateTime BirthDate { get; set; }
    public required int LotId { get; set; }
    public string? LotName { get; set; }
    public int? MotherId { get; set; }
    public string? MotherCuia { get; set; }
    public int? FatherId { get; set; }
    public string? FatherCuia { get; set; }
    public required List<AnimalOwnerDto> Owners { get; set; }
    public required List<PhotoDto> Photos { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAnimalDto
{
    public string? Cuia { get; set; }
    public required string TagVisual { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public required string Sex { get; set; }
    public required LifeStatus LifeStatus { get; set; }
    public required ProductionStatus ProductionStatus { get; set; }
    public required HealthStatus HealthStatus { get; set; }
    public required ReproductiveStatus ReproductiveStatus { get; set; }
    public required DateTime BirthDate { get; set; }
    public required int LotId { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public required List<AnimalOwnerDto> Owners { get; set; }
}

public class UpdateAnimalDto
{
    public string? Cuia { get; set; }
    public string? TagVisual { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public string? Sex { get; set; }

    public LifeStatus? LifeStatus { get; set; }
    public ProductionStatus? ProductionStatus { get; set; }
    public HealthStatus? HealthStatus { get; set; }
    public ReproductiveStatus? ReproductiveStatus { get; set; }

    public DateTime? BirthDate { get; set; }
    public int? LotId { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public List<AnimalOwnerDto>? Owners { get; set; }
}

public class AnimalOwnerDto
{
    public required int OwnerId { get; set; }
    public required string OwnerName { get; set; }
    public required decimal SharePercent { get; set; }
}

public class AnimalGenealogyDto
{
    public required int Id { get; set; }
    public string? Cuia { get; set; }
    public required string TagVisual { get; set; }
    public string? Name { get; set; }
    public required string Sex { get; set; }
    public required DateTime BirthDate { get; set; }
    public AnimalGenealogyDto? Mother { get; set; }
    public AnimalGenealogyDto? Father { get; set; }
    public required List<AnimalGenealogyDto> Children { get; set; }
}
