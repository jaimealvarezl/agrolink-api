using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Delete;

public class DeleteChecklistCommandHandler(
    IChecklistRepository checklistRepository,
    ILotRepository lotRepository,
    IPaddockRepository paddockRepository,
    ICurrentUserService currentUserService,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteChecklistCommand, Unit>
{
    public async Task<Unit> Handle(
        DeleteChecklistCommand request,
        CancellationToken cancellationToken
    )
    {
        var checklist = await checklistRepository.GetByIdAsync(request.Id);
        if (checklist == null)
        {
            throw new ArgumentException("Checklist not found");
        }

        // Security check: ensure checklist belongs to the current farm context
        if (currentUserService.CurrentFarmId.HasValue)
        {
            int? checklistFarmId = null;
            if (checklist.ScopeType == "LOT")
            {
                var lot = await lotRepository.GetLotWithPaddockAsync(checklist.ScopeId);
                checklistFarmId = lot?.Paddock?.FarmId;
            }
            else if (checklist.ScopeType == "PADDOCK")
            {
                var paddock = await paddockRepository.GetByIdAsync(checklist.ScopeId);
                checklistFarmId = paddock?.FarmId;
            }

            if (
                checklistFarmId != null
                && checklistFarmId != currentUserService.CurrentFarmId.Value
            )
            {
                throw new ForbiddenAccessException("You do not have access to this checklist");
            }
        }

        checklistRepository.Remove(checklist);
        await unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}
