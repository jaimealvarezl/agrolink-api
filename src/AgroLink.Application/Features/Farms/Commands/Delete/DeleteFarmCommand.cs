using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Delete;

public record DeleteFarmCommand(int Id, int UserId) : IRequest;

public class DeleteFarmCommandHandler(
    IFarmRepository farmRepository,
    IOwnerRepository ownerRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteFarmCommand>
{
    public async Task Handle(DeleteFarmCommand request, CancellationToken cancellationToken)
    {
        // 1. Get Farm
        var farm = await farmRepository.GetByIdAsync(request.Id);
        if (farm is not { IsActive: true })
        {
            // Idempotency: If already deleted or not found, return success.
            return;
        }

        // 2. Verify Ownership
        var owner = await ownerRepository.FirstOrDefaultAsync(o => o.UserId == request.UserId);
        if (owner == null || farm.OwnerId != owner.Id)
        {
            throw new ForbiddenAccessException("Only the owner can delete the farm.");
        }

        // 3. Soft Delete
        farm.IsActive = false;
        farm.DeletedAt = DateTime.UtcNow;
        farm.UpdatedAt = DateTime.UtcNow;

        farmRepository.Update(farm);
        await unitOfWork.SaveChangesAsync();
    }
}
