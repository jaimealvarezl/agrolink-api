using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;

namespace AgroLink.Application.Features.AnimalBrands.DTOs;

public static class AnimalBrandMappingExtensions
{
    public static AnimalBrandDto ToDto(this AnimalBrand animalBrand, IStorageService storageService)
    {
        return new AnimalBrandDto
        {
            Id = animalBrand.Id,
            AnimalId = animalBrand.AnimalId,
            OwnerBrandId = animalBrand.OwnerBrandId,
            OwnerBrand = animalBrand.OwnerBrand.ToDto(storageService),
            AppliedAt = animalBrand.AppliedAt,
            Notes = animalBrand.Notes,
            CreatedAt = animalBrand.CreatedAt,
        };
    }
}
