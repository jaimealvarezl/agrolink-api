using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Farms.Commands.Delete;

public record DeleteFarmCommand(int Id) : IRequest;

public class DeleteFarmCommandHandler(IFarmRepository farmRepository, IUnitOfWork unitOfWork)
    : IRequestHandler<DeleteFarmCommand>
{
    public async Task Handle(DeleteFarmCommand request, CancellationToken cancellationToken)
    {
        var farm = await farmRepository.GetByIdAsync(request.Id);
        if (farm == null)
        {
            throw new ArgumentException("Farm not found");
        }

        farmRepository.Remove(farm);
        await unitOfWork.SaveChangesAsync();
    }
}
