using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class UserRepository(AgroLinkDbContext context) : Repository<User>(context), IUserRepository
{
    public async Task<User?> GetByEmailAsync(string email)
    {
        return await _dbSet.AsNoTracking().FirstOrDefaultAsync(u => u.Email == email);
    }

    public async Task<IEnumerable<User>> GetActiveUsersAsync()
    {
        return await _dbSet
            .AsNoTracking()
            .Where(u => u.IsActive)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await _dbSet.AnyAsync(u => u.Email == email);
    }
}
