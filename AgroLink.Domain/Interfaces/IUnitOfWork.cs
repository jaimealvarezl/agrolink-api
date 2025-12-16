using System.Threading.Tasks;

namespace AgroLink.Domain.Interfaces
{
    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
