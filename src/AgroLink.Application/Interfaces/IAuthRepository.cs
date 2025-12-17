using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IAuthRepository
{
    Task<User?> GetUserByEmailAsync(string email);
    Task UpdateUserAsync(User user);
    Task AddUserAsync(User user);
    Task<User?> GetUserByIdAsync(int userId);
}
