using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Delete;

public record DeleteFarmCommand(int Id, int UserId) : IRequest;

public class DeleteFarmCommandHandler(IFarmRepository farmRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteFarmCommand>
{
    public async Task Handle(DeleteFarmCommand request, CancellationToken cancellationToken)
    {
        var farm = await farmRepository.GetByIdAsync(request.Id);
        if (farm is not { IsActive: true })
        {
            // Idempotency: If already deleted or not found, return success.
            return;
        }

        farm.IsActive = false;
        farm.DeletedAt = DateTime.UtcNow;
        farm.UpdatedAt = DateTime.UtcNow;

        farmRepository.Update(farm);
        await unitOfWork.SaveChangesAsync();
    }
}
