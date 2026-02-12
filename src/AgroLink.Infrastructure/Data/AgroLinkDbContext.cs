using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Data;

public class AgroLinkDbContext : DbContext
{
    public AgroLinkDbContext(DbContextOptions<AgroLinkDbContext> options)
        : base(options) { }

    public DbSet<Farm> Farms { get; set; }
    public DbSet<Paddock> Paddocks { get; set; }
    public DbSet<Lot> Lots { get; set; }
    public DbSet<Animal> Animals { get; set; }
    public DbSet<Owner> Owners { get; set; }
    public DbSet<AnimalOwner> AnimalOwners { get; set; }
    public DbSet<Movement> Movements { get; set; }
    public DbSet<Checklist> Checklists { get; set; }
    public DbSet<ChecklistItem> ChecklistItems { get; set; }
    public DbSet<AnimalPhoto> AnimalPhotos { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<FarmMember> FarmMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgroLinkDbContext).Assembly);
    }
}
