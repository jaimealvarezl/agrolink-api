using AgroLink.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace AgroLink.Infrastructure.Data;

public class UnitOfWork : IUnitOfWork
{
    private readonly AgroLinkDbContext _context;
    private IDbContextTransaction? _transaction;

    public UnitOfWork(AgroLinkDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync()
    {
        return _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        _transaction = await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.CommitAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackTransactionAsync()
    {
        if (_transaction != null)
        {
            await _transaction.RollbackAsync();
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }
}
