using AgroLink.Application.Common.Exceptions;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.DeleteNote;

public record DeleteAnimalNoteCommand(int AnimalId, int NoteId, int UserId) : IRequest;

public class DeleteAnimalNoteCommandHandler(
    IAnimalNoteRepository animalNoteRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<DeleteAnimalNoteCommand>
{
    public async Task Handle(DeleteAnimalNoteCommand request, CancellationToken cancellationToken)
    {
        var note = await animalNoteRepository.GetByIdAsync(request.NoteId);
        if (note == null || note.AnimalId != request.AnimalId)
        {
            throw new NotFoundException($"Note with ID {request.NoteId} was not found.");
        }

        if (note.UserId != request.UserId)
        {
            throw new ForbiddenAccessException("You can only delete your own notes.");
        }

        animalNoteRepository.Remove(note);
        await unitOfWork.SaveChangesAsync();
    }
}
