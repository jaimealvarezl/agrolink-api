using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetNotes;

public record GetAnimalNotesQuery(int AnimalId, int FarmId) : IRequest<IEnumerable<AnimalNoteDto>>;

public class GetAnimalNotesQueryHandler(
    IAnimalRepository animalRepository,
    IAnimalNoteRepository animalNoteRepository
) : IRequestHandler<GetAnimalNotesQuery, IEnumerable<AnimalNoteDto>>
{
    public async Task<IEnumerable<AnimalNoteDto>> Handle(
        GetAnimalNotesQuery request,
        CancellationToken cancellationToken
    )
    {
        _ =
            await animalRepository.GetByIdInFarmAsync(request.AnimalId, request.FarmId)
            ?? throw new NotFoundException("Animal", request.AnimalId);

        var notes = await animalNoteRepository.GetByAnimalIdAsync(request.AnimalId);

        return notes.Select(n => new AnimalNoteDto
        {
            Id = n.Id,
            AnimalId = n.AnimalId,
            Content = n.Content,
            UserId = n.UserId,
            UserName = n.User?.Name ?? string.Empty,
            CreatedAt = n.CreatedAt,
        });
    }
}
