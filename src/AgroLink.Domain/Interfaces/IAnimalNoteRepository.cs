using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalNoteRepository
{
    Task<IEnumerable<AnimalNote>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    );

    Task<AnimalNote?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task AddAsync(AnimalNote note, CancellationToken cancellationToken);
    void Remove(AnimalNote note);
}
