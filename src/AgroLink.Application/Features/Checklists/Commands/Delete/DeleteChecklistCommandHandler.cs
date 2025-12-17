using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Delete;

public class DeleteChecklistCommandHandler(
    IChecklistRepository checklistRepository,
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

        checklistRepository.Remove(checklist);
        await unitOfWork.SaveChangesAsync();

        return Unit.Value;
    }
}
