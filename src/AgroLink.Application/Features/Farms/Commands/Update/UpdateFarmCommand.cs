using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Update;

public record UpdateFarmCommand(int Id, string Name, string? Location, string? CUE)
    : IRequest<FarmDto>;

public class UpdateFarmCommandHandler(
    IFarmRepository farmRepository,
    IOwnerRepository ownerRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<UpdateFarmCommand, FarmDto>
{
    public async Task<FarmDto> Handle(
        UpdateFarmCommand request,
        CancellationToken cancellationToken
    )
    {
        // 1. Get Current User and Owner record
        var userId = currentUserService.GetRequiredUserId();
        var owner = await ownerRepository.FirstOrDefaultAsync(o => o.UserId == userId);

        if (owner == null)
        {
            throw new ForbiddenAccessException("User is not registered as an Owner.");
        }

        // 2. Get Farm and verify ownership
        var farm = await farmRepository.GetByIdAsync(request.Id);
        if (farm == null)
        {
            throw new ArgumentException("Farm not found");
        }

        if (farm.OwnerId != owner.Id)
        {
            throw new ForbiddenAccessException("Only the owner can update the farm details.");
        }

        // 3. Update fields (Format validation handled by API layer/Annotations)
        farm.Name = request.Name;
        farm.Location = request.Location;
        farm.CUE = request.CUE;
        farm.UpdatedAt = DateTime.UtcNow;

        farmRepository.Update(farm);
        await unitOfWork.SaveChangesAsync();

        return new FarmDto
        {
            Id = farm.Id,
            Name = farm.Name,
            Location = farm.Location,
            CUE = farm.CUE,
            OwnerId = farm.OwnerId,
            Role = "Owner",
            CreatedAt = farm.CreatedAt,
        };
    }
}
