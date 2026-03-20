using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Commands.CreateNote;

public record CreateAnimalNoteCommand(int FarmId, int AnimalId, string Content, int UserId)
    : IRequest<AnimalNoteDto>;

public class CreateAnimalNoteCommandHandler(
    IAnimalRepository animalRepository,
    IAnimalNoteRepository animalNoteRepository,
    IUserRepository userRepository,
    IUnitOfWork unitOfWork
) : IRequestHandler<CreateAnimalNoteCommand, AnimalNoteDto>
{
    public async Task<AnimalNoteDto> Handle(
        CreateAnimalNoteCommand request,
        CancellationToken cancellationToken
    )
    {
        _ =
            await animalRepository.GetByIdInFarmAsync(request.AnimalId, request.FarmId)
            ?? throw new NotFoundException("Animal", request.AnimalId);

        var note = new AnimalNote
        {
            AnimalId = request.AnimalId,
            Content = request.Content,
            UserId = request.UserId,
            CreatedAt = DateTime.UtcNow,
        };

        await animalNoteRepository.AddAsync(note);
        await unitOfWork.SaveChangesAsync();

        var user = await userRepository.GetByIdAsync(request.UserId);

        return new AnimalNoteDto
        {
            Id = note.Id,
            AnimalId = note.AnimalId,
            Content = note.Content,
            UserId = note.UserId,
            UserName = user?.Name ?? string.Empty,
            CreatedAt = note.CreatedAt,
        };
    }
}
