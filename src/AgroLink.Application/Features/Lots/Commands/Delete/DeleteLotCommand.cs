using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Lots.Commands.Delete;

public record DeleteLotCommand(int Id) : IRequest;

public class DeleteLotCommandHandler(
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteLotCommand>
{
    public async Task Handle(DeleteLotCommand request, CancellationToken cancellationToken)
    {
        var lot = await lotRepository.GetByIdAsync(request.Id);
        if (lot == null)
        {
            throw new ArgumentException("Lot not found");
        }

        // Security check: ensure lot belongs to the current farm context
        if (currentUserService.CurrentFarmId.HasValue)
        {
            var paddockContext = await paddockRepository.GetByIdAsync(lot.PaddockId);
            if (
                paddockContext != null
                && paddockContext.FarmId != currentUserService.CurrentFarmId.Value
            )
            {
                throw new ForbiddenAccessException("You do not have access to this lot");
            }
        }

        lotRepository.Remove(lot);
        await unitOfWork.SaveChangesAsync();
    }
}
