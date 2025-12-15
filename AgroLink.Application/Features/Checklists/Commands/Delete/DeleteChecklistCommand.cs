using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Commands.Delete;

public record DeleteChecklistCommand(int Id) : IRequest;

public class DeleteChecklistCommandHandler(IChecklistRepository checklistRepository)
    : IRequestHandler<DeleteChecklistCommand>
{
    public async Task Handle(DeleteChecklistCommand request, CancellationToken cancellationToken)
    {
        var checklist = await checklistRepository.GetByIdAsync(request.Id);
        if (checklist == null)
        {
            throw new ArgumentException("Checklist not found");
        }

        checklistRepository.Remove(checklist);
        await checklistRepository.SaveChangesAsync();
    }
}
