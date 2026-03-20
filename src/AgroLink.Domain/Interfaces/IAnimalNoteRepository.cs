using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IAnimalNoteRepository
{
    Task<IEnumerable<AnimalNote>> GetByAnimalIdAsync(int animalId);
    Task<AnimalNote?> GetByIdAsync(int id);
    Task AddAsync(AnimalNote note);
    void Remove(AnimalNote note);
}
