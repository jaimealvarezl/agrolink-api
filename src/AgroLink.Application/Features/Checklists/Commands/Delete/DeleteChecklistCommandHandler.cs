using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Delete;

public class DeleteChecklistCommandHandler(
    IChecklistRepository checklistRepository,
    ILotRepository lotRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteChecklistCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteChecklistCommand request,
        CancellationToken cancellationToken
    )
    {
        var checklist =
            await checklistRepository.GetByIdAsync(request.Id)
            ?? throw new NotFoundException($"Checklist with ID {request.Id} was not found.");

        var farmId =
            currentUserService.CurrentFarmId
            ?? throw new UnauthorizedAccessException(
                "Farm context is required to delete a checklist."
            );

        var lot =
            await lotRepository.GetLotWithPaddockAsync(checklist.LotId)
            ?? throw new NotFoundException($"Lot with ID {checklist.LotId} was not found.");
        if (lot.Paddock?.FarmId != farmId)
        {
            throw new ForbiddenAccessException("You do not have access to this checklist.");
        }

        checklistRepository.Remove(checklist);
        await unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}
