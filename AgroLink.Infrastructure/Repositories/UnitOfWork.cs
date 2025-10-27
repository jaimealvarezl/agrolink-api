using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly AgroLinkDbContext _context;
    private readonly Dictionary<Type, object> _repositories;

    public UnitOfWork(AgroLinkDbContext context)
    {
        _context = context;
        _repositories = new Dictionary<Type, object>();
    }

    public IRepository<Farm> Farms => GetRepository<Farm>();
    public IRepository<Paddock> Paddocks => GetRepository<Paddock>();
    public IRepository<Lot> Lots => GetRepository<Lot>();
    public IRepository<Animal> Animals => GetRepository<Animal>();
    public IRepository<Owner> Owners => GetRepository<Owner>();
    public IRepository<AnimalOwner> AnimalOwners => GetRepository<AnimalOwner>();
    public IRepository<Movement> Movements => GetRepository<Movement>();
    public IRepository<Checklist> Checklists => GetRepository<Checklist>();
    public IRepository<ChecklistItem> ChecklistItems => GetRepository<ChecklistItem>();
    public IRepository<Photo> Photos => GetRepository<Photo>();
    public IRepository<User> Users => GetRepository<User>();

    private IRepository<T> GetRepository<T>() where T : class
    {
        var type = typeof(T);
        if (!_repositories.ContainsKey(type))
        {
            _repositories[type] = new Repository<T>(_context);
        }
        return (IRepository<T>)_repositories[type];
    }

    public async Task<int> SaveChangesAsync()
    {
        return await _context.SaveChangesAsync();
    }

    public async Task BeginTransactionAsync()
    {
        await _context.Database.BeginTransactionAsync();
    }

    public async Task CommitTransactionAsync()
    {
        await _context.Database.CommitTransactionAsync();
    }

    public async Task RollbackTransactionAsync()
    {
        await _context.Database.RollbackTransactionAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}