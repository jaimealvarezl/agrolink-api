using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;

namespace AgroLink.Application.Features.OwnerBrands.DTOs;

public static class OwnerBrandMappingExtensions
{
    public static OwnerBrandDto ToDto(this OwnerBrand brand, IStorageService storageService)
    {
        return new OwnerBrandDto
        {
            Id = brand.Id,
            OwnerId = brand.OwnerId,
            Description = brand.Description,
            PhotoUrl =
                brand.PhotoStorageKey != null
                    ? storageService.GetPresignedUrl(brand.PhotoStorageKey, TimeSpan.FromHours(1))
                    : null,
            IsActive = brand.IsActive,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt,
        };
    }
}
