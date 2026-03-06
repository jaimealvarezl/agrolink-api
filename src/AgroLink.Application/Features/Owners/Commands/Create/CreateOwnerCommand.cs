using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Owners.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Owners.Commands.Create;

public record CreateOwnerCommand(
    int FarmId,
    string Name,
    string? Phone,
    string? Email,
    int? RequestUserId
) : IRequest<OwnerDto>;

public class CreateOwnerCommandHandler(
    IOwnerRepository ownerRepository,
    IFarmRepository farmRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateOwnerCommand, OwnerDto>
{
    public async Task<OwnerDto> Handle(
        CreateOwnerCommand request,
        CancellationToken cancellationToken
    )
    {
        var farmExists = await farmRepository.ExistsAsync(f => f.Id == request.FarmId);
        if (!farmExists)
        {
            throw new NotFoundException($"Farm with ID {request.FarmId} not found.");
        }

        var existingOwner = await ownerRepository.FirstOrDefaultIgnoreFiltersAsync(o =>
            o.FarmId == request.FarmId && o.Name == request.Name
        );

        if (existingOwner != null)
        {
            if (!existingOwner.IsActive)
            {
                // Restore soft-deleted owner
                existingOwner.IsActive = true;
                existingOwner.Phone = request.Phone ?? existingOwner.Phone;
                existingOwner.Email = request.Email ?? existingOwner.Email;
                existingOwner.UserId = request.RequestUserId ?? existingOwner.UserId;
                existingOwner.UpdatedAt = DateTime.UtcNow;

                ownerRepository.Update(existingOwner);
                await unitOfWork.SaveChangesAsync();

                return MapToDto(existingOwner);
            }

            throw new ArgumentException(
                $"Owner with name '{request.Name}' already exists in this farm."
            );
        }

        var owner = new Owner
        {
            Name = request.Name,
            Phone = request.Phone,
            Email = request.Email,
            UserId = request.RequestUserId,
            FarmId = request.FarmId,
            IsActive = true,
        };

        await ownerRepository.AddAsync(owner);
        await unitOfWork.SaveChangesAsync();

        return MapToDto(owner);
    }

    private static OwnerDto MapToDto(Owner owner)
    {
        return new OwnerDto
        {
            Id = owner.Id,
            Name = owner.Name,
            Phone = owner.Phone,
            Email = owner.Email,
            UserId = owner.UserId,
            IsActive = owner.IsActive,
            CreatedAt = owner.CreatedAt,
        };
    }
}
