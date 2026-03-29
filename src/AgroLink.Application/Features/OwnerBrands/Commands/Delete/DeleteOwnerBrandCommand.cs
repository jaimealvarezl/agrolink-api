using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.OwnerBrands.Commands.Delete;

public record DeleteOwnerBrandCommand(int FarmId, int OwnerId, int BrandId) : IRequest;

public class DeleteOwnerBrandCommandHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IStorageService storageService,
    IUnitOfWork unitOfWork,
    ILogger<DeleteOwnerBrandCommandHandler> logger
) : IRequestHandler<DeleteOwnerBrandCommand>
{
    public async Task Handle(DeleteOwnerBrandCommand request, CancellationToken cancellationToken)
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

        var brand = await ownerBrandRepository.FirstOrDefaultAsync(b =>
            b.Id == request.BrandId && b.OwnerId == request.OwnerId
        );
        if (brand == null)
        {
            throw new NotFoundException(
                $"Brand with ID {request.BrandId} not found for owner {request.OwnerId}."
            );
        }

        var photoStorageKey = brand.PhotoStorageKey;

        brand.IsActive = false;
        brand.UpdatedAt = DateTime.UtcNow;

        ownerBrandRepository.Update(brand);
        await unitOfWork.SaveChangesAsync();

        if (photoStorageKey != null)
        {
            try
            {
                await storageService.DeleteFileAsync(photoStorageKey);
            }
            catch (Exception ex)
            {
                logger.LogWarning(
                    ex,
                    "Failed to delete brand photo from storage. Key: {Key}",
                    photoStorageKey
                );
            }
        }
    }
}
