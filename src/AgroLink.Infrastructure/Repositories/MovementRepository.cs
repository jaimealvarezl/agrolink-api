using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class MovementRepository(AgroLinkDbContext context) : IMovementRepository
{
    public async Task<IEnumerable<Movement>> GetMovementsByAnimalAsync(int animalId)
    {
        return await context
            .Movements.AsNoTracking()
            .Where(m => m.AnimalId == animalId)
            .ToListAsync();
    }

    public async Task AddMovementAsync(Movement movement)
    {
        context.Movements.Add(movement);
        await context.SaveChangesAsync();
    }
}
