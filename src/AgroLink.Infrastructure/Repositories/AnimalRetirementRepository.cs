using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AnimalRetirementRepository(AgroLinkDbContext context) : IAnimalRetirementRepository
{
    public async Task<AnimalRetirement?> GetByAnimalIdAsync(int animalId)
    {
        return await context
            .AnimalRetirements.AsNoTracking()
            .Include(r => r.User)
            .FirstOrDefaultAsync(r => r.AnimalId == animalId);
    }

    public async Task AddAsync(AnimalRetirement retirement)
    {
        await context.AnimalRetirements.AddAsync(retirement);
    }
}
