using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.OwnerBrands.Commands.Update;

public record UpdateOwnerBrandCommand(
    int FarmId,
    int OwnerId,
    int BrandId,
    string RegistrationNumber,
    string Description,
    string? PhotoUrl
) : IRequest<OwnerBrandDto>;

public class UpdateOwnerBrandCommandHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateOwnerBrandCommand, OwnerBrandDto>
{
    public async Task<OwnerBrandDto> Handle(
        UpdateOwnerBrandCommand request,
        CancellationToken cancellationToken
    )
    {
        var ownerExists = await ownerRepository.ExistsAsync(o =>
            o.Id == request.OwnerId && o.FarmId == request.FarmId
        );
        if (!ownerExists)
            throw new NotFoundException(
                $"Owner with ID {request.OwnerId} not found in farm {request.FarmId}."
            );

        var brand = await ownerBrandRepository.FirstOrDefaultAsync(b =>
            b.Id == request.BrandId && b.OwnerId == request.OwnerId
        );
        if (brand == null)
            throw new NotFoundException(
                $"Brand with ID {request.BrandId} not found for owner {request.OwnerId}."
            );

        var duplicate = await ownerBrandRepository.ExistsAsync(b =>
            b.OwnerId == request.OwnerId
            && b.RegistrationNumber == request.RegistrationNumber
            && b.Id != request.BrandId
        );
        if (duplicate)
            throw new ArgumentException(
                $"Brand with registration number '{request.RegistrationNumber}' already exists for this owner."
            );

        brand.RegistrationNumber = request.RegistrationNumber;
        brand.Description = request.Description;
        brand.PhotoUrl = request.PhotoUrl;
        brand.UpdatedAt = DateTime.UtcNow;

        ownerBrandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync();

        return new OwnerBrandDto
        {
            Id = brand.Id,
            OwnerId = brand.OwnerId,
            RegistrationNumber = brand.RegistrationNumber,
            Description = brand.Description,
            PhotoUrl = brand.PhotoUrl,
            IsActive = brand.IsActive,
            CreatedAt = brand.CreatedAt,
            UpdatedAt = brand.UpdatedAt,
        };
    }
}
