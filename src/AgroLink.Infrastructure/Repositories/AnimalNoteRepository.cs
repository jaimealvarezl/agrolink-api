using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalNoteRepository(AgroLinkDbContext context) : IAnimalNoteRepository
{
    public async Task<IEnumerable<AnimalNote>> GetByAnimalIdAsync(
        int animalId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .AnimalNotes.AsNoTracking()
            .Include(n => n.User)
            .Where(n => n.AnimalId == animalId)
            .OrderByDescending(n => n.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<AnimalNote?> GetByIdAsync(
        int id,
        CancellationToken cancellationToken = default
    )
    {
        return await context.AnimalNotes.FindAsync([id], cancellationToken);
    }

    public async Task AddAsync(AnimalNote note, CancellationToken cancellationToken)
    {
        await context.AnimalNotes.AddAsync(note, cancellationToken);
    }

    public void Remove(AnimalNote note)
    {
        context.AnimalNotes.Remove(note);
    }
}
