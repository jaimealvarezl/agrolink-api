using System.ComponentModel.DataAnnotations;
using AgroLink.Domain.Enums;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalDto
{
    public required int Id { get; set; }
    public string? Cuia { get; set; }
    public string? TagVisual { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Color { get; set; }
    public string? Breed { get; set; }

    [Required]
    public required Sex Sex { get; set; }

    [Required]
    public required LifeStatus LifeStatus { get; set; }

    [Required]
    public required ProductionStatus ProductionStatus { get; set; }

    [Required]
    public required HealthStatus HealthStatus { get; set; }

    [Required]
    public required ReproductiveStatus ReproductiveStatus { get; set; }

    [Required]
    public required DateTime BirthDate { get; set; }

    [Required]
    public required int LotId { get; set; }

    public string? LotName { get; set; }
    public int? MotherId { get; set; }
    public string? MotherCuia { get; set; }
    public int? FatherId { get; set; }
    public string? FatherCuia { get; set; }

    [Required]
    public required List<AnimalOwnerDto> Owners { get; set; }

    [Required]
    public required List<AnimalPhotoDto> Photos { get; set; }

    [Required]
    public required DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
}

public class CreateAnimalDto
{
    public string? Cuia { get; set; }
    public string? TagVisual { get; set; }

    [Required]
    public required string Name { get; set; }

    public string? Color { get; set; }
    public string? Breed { get; set; }

    [Required]
    public required Sex Sex { get; set; }

    [Required]
    public required LifeStatus LifeStatus { get; set; }

    [Required]
    public required ProductionStatus ProductionStatus { get; set; }

    [Required]
    public required HealthStatus HealthStatus { get; set; }

    [Required]
    public required ReproductiveStatus ReproductiveStatus { get; set; }

    [Required]
    public required DateTime BirthDate { get; set; }

    [Required]
    public required int LotId { get; set; }

    public int? MotherId { get; set; }
    public int? FatherId { get; set; }

    [Required]
    public required List<AnimalOwnerCreateDto> Owners { get; set; }
}

public class UpdateAnimalDto
{
    public string? Cuia { get; set; }
    public string? TagVisual { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public Sex? Sex { get; set; }

    public LifeStatus? LifeStatus { get; set; }
    public ProductionStatus? ProductionStatus { get; set; }
    public HealthStatus? HealthStatus { get; set; }
    public ReproductiveStatus? ReproductiveStatus { get; set; }

    public DateTime? BirthDate { get; set; }
    public int? LotId { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public List<AnimalOwnerCreateDto>? Owners { get; set; }
}

public class AnimalOwnerDto
{
    [Required]
    public required int OwnerId { get; set; }

    [Required]
    public required string OwnerName { get; set; }

    [Required]
    public required decimal SharePercent { get; set; }
}

public class AnimalOwnerCreateDto
{
    [Required]
    public required int OwnerId { get; set; }

    [Required]
    public required decimal SharePercent { get; set; }
}

public class AnimalGenealogyDto
{
    public required int Id { get; set; }
    public string? Cuia { get; set; }
    public string? TagVisual { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required Sex Sex { get; set; }

    [Required]
    public required DateTime BirthDate { get; set; }

    public AnimalGenealogyDto? Mother { get; set; }
    public AnimalGenealogyDto? Father { get; set; }

    [Required]
    public required List<AnimalGenealogyDto> Children { get; set; }
}
