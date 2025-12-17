using AgroLink.Application.Features.Photos.DTOs;

namespace AgroLink.Application.Features.Animals.DTOs;

public class AnimalDto
{
    public int Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public string Sex { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int LotId { get; set; }
    public string? LotName { get; set; }
    public int? MotherId { get; set; }
    public string? MotherTag { get; set; }
    public int? FatherId { get; set; }
    public string? FatherTag { get; set; }
    public List<AnimalOwnerDto> Owners { get; set; } = new();
    public List<PhotoDto> Photos { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CreateAnimalDto
{
    public string Tag { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public string Sex { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int LotId { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public List<AnimalOwnerDto> Owners { get; set; } = new();
}

public class UpdateAnimalDto
{
    public string? Name { get; set; }
    public string? Color { get; set; }
    public string? Breed { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public int? MotherId { get; set; }
    public int? FatherId { get; set; }
    public List<AnimalOwnerDto> Owners { get; set; } = new();
}

public class AnimalOwnerDto
{
    public int OwnerId { get; set; }
    public string OwnerName { get; set; } = string.Empty;
    public decimal SharePercent { get; set; }
}

public class AnimalGenealogyDto
{
    public int Id { get; set; }
    public string Tag { get; set; } = string.Empty;
    public string? Name { get; set; }
    public string Sex { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public AnimalGenealogyDto? Mother { get; set; }
    public AnimalGenealogyDto? Father { get; set; }
    public List<AnimalGenealogyDto> Children { get; set; } = new();
}
