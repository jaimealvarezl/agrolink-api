using AgroLink.Domain.Interfaces;

namespace AgroLink.Infrastructure.Data
{
    public class UnitOfWork : IUnitOfWork
    {
        private readonly AgroLinkDbContext _context;

        public UnitOfWork(AgroLinkDbContext context)
        {
            _context = context;
        }

        public Task<int> SaveChangesAsync()
        {
            return _context.SaveChangesAsync();
        }
    }
}
