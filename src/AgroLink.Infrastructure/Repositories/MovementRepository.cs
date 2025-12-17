using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class MovementRepository(AgroLinkDbContext context) : IMovementRepository
{
    public async Task<IEnumerable<Movement>> GetMovementsByEntityAsync(
        string entityType,
        int entityId
    )
    {
        return await context
            .Movements.Where(m => m.EntityType == entityType && m.EntityId == entityId)
            .ToListAsync();
    }

    public async Task AddMovementAsync(Movement movement)
    {
        context.Movements.Add(movement);
        await context.SaveChangesAsync();
    }

    public async Task<User?> GetUserByIdAsync(int userId)
    {
        return await context.Users.FindAsync(userId);
    }

    public async Task<Animal?> GetAnimalByIdAsync(int animalId)
    {
        return await context.Animals.FindAsync(animalId);
    }

    public async Task<Lot?> GetLotByIdAsync(int lotId)
    {
        return await context.Lots.FindAsync(lotId);
    }

    public async Task<Paddock?> GetPaddockByIdAsync(int paddockId)
    {
        return await context.Paddocks.FindAsync(paddockId);
    }
}
