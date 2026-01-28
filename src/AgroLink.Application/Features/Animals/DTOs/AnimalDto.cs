using AgroLink.Application.Features.Photos.DTOs;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalDto
{
    public required int Id { get; set; }
    public required string Tag { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public required string Sex { get; set; }
    public required string Status { get; set; }
    public DateTime? BirthDate { get; set; }
    public required int LotId { get; set; }
    public string? LotName { get; set; }
    public int? MotherId { get; set; }
    public string? MotherTag { get; set; }
    public int? FatherId { get; set; }
    public string? FatherTag { get; set; }
    public required List<AnimalOwnerDto> Owners { get; set; }
    public required List<PhotoDto> Photos { get; set; }
    public required DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAnimalDto
{
    public required string Tag { get; set; }
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public required string Sex { get; set; }
    public DateTime? BirthDate { get; set; }
    public required int LotId { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public List<AnimalOwnerDto> Owners { get; set; } = new();
}

public class UpdateAnimalDto
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public string? Status { get; set; }
    public DateTime? BirthDate { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public List<AnimalOwnerDto> Owners { get; set; } = new();
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
    public required string Tag { get; set; }
    public string? Name { get; set; }
    public required string Sex { get; set; }
    public DateTime? BirthDate { get; set; }
    public AnimalGenealogyDto? Mother { get; set; }
    public AnimalGenealogyDto? Father { get; set; }
    public required List<AnimalGenealogyDto> Children { get; set; }
}
