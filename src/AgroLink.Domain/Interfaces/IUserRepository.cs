using AgroLink.Domain.Entities;

namespace AgroLink.Domain.Interfaces;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByEmailAsync(string email);
}
