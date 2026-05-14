using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<User?> GetUserByFirebaseUidAsync(
        string firebaseUid,
        CancellationToken cancellationToken = default
    );

    Task UpdateUserAsync(User user, CancellationToken cancellationToken = default);
    Task AddUserAsync(User user, CancellationToken cancellationToken = default);
    Task<User?> GetUserByIdAsync(int userId, CancellationToken cancellationToken = default);
}
