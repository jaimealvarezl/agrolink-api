using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.OwnerBrands.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace AgroLink.Application.Features.OwnerBrands.Commands.UploadPhoto;

public record UploadOwnerBrandPhotoCommand(
    int FarmId,
    int OwnerId,
    int BrandId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long Size
) : IRequest<OwnerBrandDto>;

public class UploadOwnerBrandPhotoCommandHandler(
    IOwnerBrandRepository ownerBrandRepository,
    IOwnerRepository ownerRepository,
    IStorageService storageService,
    IStoragePathProvider pathProvider,
    IUnitOfWork unitOfWork,
    ILogger<UploadOwnerBrandPhotoCommandHandler> logger
) : IRequestHandler<UploadOwnerBrandPhotoCommand, OwnerBrandDto>
{
    public async Task<OwnerBrandDto> Handle(
        UploadOwnerBrandPhotoCommand request,
        CancellationToken cancellationToken
    )
    {
        var seekableStream = await ImageFileValidator.ValidateAsync(
            request.FileStream,
            request.FileName,
            request.ContentType,
            request.Size,
            cancellationToken
        );

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

        var oldStorageKey = brand.PhotoStorageKey;
        var newKey = pathProvider.GetOwnerBrandPhotoPath(
            request.FarmId,
            request.BrandId,
            request.FileName
        );

        try
        {
            await storageService.UploadFileAsync(
                newKey,
                seekableStream,
                request.ContentType,
                request.Size
            );

            brand.PhotoUrl = storageService.GetFileUrl(newKey);
            brand.PhotoStorageKey = newKey;
            brand.UpdatedAt = DateTime.UtcNow;

            ownerBrandRepository.Update(brand);
            await unitOfWork.SaveChangesAsync();

            if (oldStorageKey != null)
            {
                try
                {
                    await storageService.DeleteFileAsync(oldStorageKey);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Failed to delete old brand photo. Key: {Key}",
                        oldStorageKey
                    );
                }
            }
        }
        catch (Exception ex) when (ex is not NotFoundException && ex is not ArgumentException)
        {
            logger.LogError(
                ex,
                "Failed to upload brand photo for brand {BrandId}. Key: {Key}",
                request.BrandId,
                newKey
            );
            throw new InvalidOperationException(
                $"Failed to upload photo to storage: {ex.Message}",
                ex
            );
        }

        return brand.ToDto(storageService);
    }
}
