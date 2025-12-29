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
    public DbSet<Photo> Photos { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<FarmMember> FarmMembers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Farm>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Location).HasMaxLength(500);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Paddock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity
                .HasOne(e => e.Farm)
                .WithMany(f => f.Paddocks)
                .HasForeignKey(e => e.FarmId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Lot>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);
            entity
                .HasOne(e => e.Paddock)
                .WithMany(p => p.Lots)
                .HasForeignKey(e => e.PaddockId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Animal>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Tag).IsRequired().HasMaxLength(50);
            entity.Property(e => e.Name).HasMaxLength(200);
            entity.Property(e => e.Color).HasMaxLength(100);
            entity.Property(e => e.Breed).HasMaxLength(100);
            entity.Property(e => e.Sex).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Status).IsRequired().HasMaxLength(50);

            entity
                .HasOne(e => e.Lot)
                .WithMany(l => l.Animals)
                .HasForeignKey(e => e.LotId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(e => e.Mother)
                .WithMany(a => a.Children)
                .HasForeignKey(e => e.MotherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity
                .HasOne(e => e.Father)
                .WithMany()
                .HasForeignKey(e => e.FatherId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => e.Tag).IsUnique();
            entity.HasIndex(e => e.Name);
        });

        modelBuilder.Entity<Owner>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Phone).HasMaxLength(20);
            entity.HasIndex(e => e.Name);
            entity.HasIndex(e => e.UserId).IsUnique();
        });

        modelBuilder.Entity<AnimalOwner>(entity =>
        {
            entity.HasKey(e => new { e.AnimalId, e.OwnerId });
            entity.Property(e => e.SharePercent).HasPrecision(5, 2);

            entity
                .HasOne(e => e.Animal)
                .WithMany(a => a.AnimalOwners)
                .HasForeignKey(e => e.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Owner)
                .WithMany(o => o.AnimalOwners)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Movement>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Reason).HasMaxLength(500);

            entity
                .HasOne(e => e.User)
                .WithMany(u => u.Movements)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        modelBuilder.Entity<Checklist>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.ScopeType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.Notes).HasMaxLength(1000);

            entity
                .HasOne(e => e.User)
                .WithMany(u => u.Checklists)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            entity.HasIndex(e => new
            {
                e.ScopeType,
                e.ScopeId,
                e.Date,
            });
        });

        modelBuilder.Entity<ChecklistItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Condition).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Notes).HasMaxLength(500);

            entity
                .HasOne(e => e.Checklist)
                .WithMany(c => c.ChecklistItems)
                .HasForeignKey(e => e.ChecklistId)
                .OnDelete(DeleteBehavior.Cascade);

            entity
                .HasOne(e => e.Animal)
                .WithMany(a => a.ChecklistItems)
                .HasForeignKey(e => e.AnimalId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<Photo>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.EntityType).IsRequired().HasMaxLength(20);
            entity.Property(e => e.UriLocal).IsRequired().HasMaxLength(500);
            entity.Property(e => e.UriRemote).HasMaxLength(500);
            entity.Property(e => e.Description).HasMaxLength(200);

            entity.HasIndex(e => new { e.EntityType, e.EntityId });
        });

        // Configure User
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
            entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).IsRequired().HasMaxLength(50);

            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
