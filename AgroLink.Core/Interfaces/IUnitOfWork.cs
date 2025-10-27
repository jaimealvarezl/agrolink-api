using AgroLink.Core.Entities;

namespace AgroLink.Core.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRepository<Farm> Farms { get; }
    IRepository<Paddock> Paddocks { get; }
    IRepository<Lot> Lots { get; }
    IRepository<Animal> Animals { get; }
    IRepository<Owner> Owners { get; }
    IRepository<AnimalOwner> AnimalOwners { get; }
    IRepository<Movement> Movements { get; }
    IRepository<Checklist> Checklists { get; }
    IRepository<ChecklistItem> ChecklistItems { get; }
    IRepository<Photo> Photos { get; }
    IRepository<User> Users { get; }
    
    Task<int> SaveChangesAsync();
    Task BeginTransactionAsync();
    Task CommitTransactionAsync();
    Task RollbackTransactionAsync();
}