using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Owners.Commands.Delete;

public record DeleteOwnerCommand(int FarmId, int OwnerId) : IRequest;

public class DeleteOwnerCommandHandler(
    IOwnerRepository ownerRepository,
    IFarmRepository farmRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteOwnerCommand>
{
    public async Task Handle(DeleteOwnerCommand request, CancellationToken cancellationToken)
    {
        var farm = await farmRepository.GetByIdAsync(request.FarmId);
        if (farm == null)
        {
            throw new NotFoundException($"Farm with ID {request.FarmId} not found.");
        }

        if (farm.OwnerId == request.OwnerId)
        {
            throw new ArgumentException("Cannot delete the main owner of the farm.");
        }

        var owner = await ownerRepository.FirstOrDefaultAsync(o =>
            o.Id == request.OwnerId && o.FarmId == request.FarmId
        );

        if (owner == null)
        {
            throw new NotFoundException($"Owner with ID {request.OwnerId} not found in this farm.");
        }

        owner.IsActive = false;
        owner.UpdatedAt = DateTime.UtcNow;

        ownerRepository.Update(owner);
        await unitOfWork.SaveChangesAsync();
    }
}
