using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.OwnerBrands.Commands.Create;

public record CreateOwnerBrandCommand(int FarmId, int OwnerId, string Description)
    : IRequest<OwnerBrandDto>;

public class CreateOwnerBrandCommandHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateOwnerBrandCommand, OwnerBrandDto>
{
    public async Task<OwnerBrandDto> Handle(
        CreateOwnerBrandCommand request,
        CancellationToken cancellationToken
    )
    {
        var ownerExists = await ownerRepository.ExistsAsync(o =>
            o.Id == request.OwnerId && o.FarmId == request.FarmId
        );
        if (!ownerExists)
        {
            throw new NotFoundException(
                $"Owner with ID {request.OwnerId} not found in farm {request.FarmId}."
            );
        }

        var brand = new OwnerBrand
        {
            OwnerId = request.OwnerId,
            Description = request.Description,
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        await ownerBrandRepository.AddAsync(brand);
        await unitOfWork.SaveChangesAsync();

        return MapToDto(brand);
    }

    private static OwnerBrandDto MapToDto(OwnerBrand b)
    {
        return new OwnerBrandDto
        {
            Id = b.Id,
            OwnerId = b.OwnerId,
            Description = b.Description,
            PhotoUrl = b.PhotoUrl,
            IsActive = b.IsActive,
            CreatedAt = b.CreatedAt,
            UpdatedAt = b.UpdatedAt,
        };
    }
}
