using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class AuthRepository(AgroLinkDbContext context) : IAuthRepository
{
    public async Task<User?> GetUserByEmailAsync(
        string email,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Email == email, cancellationToken);
    }

    public async Task<User?> GetUserByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.FirebaseUid == firebaseUid, cancellationToken);
    }

    public async Task UpdateUserAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task AddUserAsync(User user, CancellationToken cancellationToken = default)
    {
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetUserByIdAsync(
        int userId,
        CancellationToken cancellationToken = default
    )
    {
        return await context
            .Users.AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
    }
}
