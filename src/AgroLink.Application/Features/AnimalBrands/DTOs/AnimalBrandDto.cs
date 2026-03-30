using AgroLink.Application.Features.OwnerBrands.DTOs;

namespace AgroLink.Application.Features.AnimalBrands.DTOs;

public class AnimalBrandDto
{
    public required int Id { get; set; }
    public required int AnimalId { get; set; }
    public required int OwnerBrandId { get; set; }
    public required OwnerBrandDto OwnerBrand { get; set; }
    public DateTime? AppliedAt { get; set; }
    public string? Notes { get; set; }
    public required DateTime CreatedAt { get; set; }
}
