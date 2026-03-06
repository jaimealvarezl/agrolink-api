using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Owners.Commands.Delete;

public record DeleteOwnerCommand(int FarmId, int OwnerId, int UserId) : IRequest;

public class DeleteOwnerCommandHandler(IOwnerRepository ownerRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteOwnerCommand>
{
    public async Task Handle(DeleteOwnerCommand request, CancellationToken cancellationToken)
    {
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
