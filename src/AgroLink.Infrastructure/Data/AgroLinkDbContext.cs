using AgroLink.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Infrastructure.Data;

public class AgroLinkDbContext(DbContextOptions<AgroLinkDbContext> options) : DbContext(options)
{
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
    public DbSet<AnimalNote> AnimalNotes { get; set; }
    public DbSet<AnimalRetirement> AnimalRetirements { get; set; }
    public DbSet<OwnerBrand> OwnerBrands { get; set; }
    public DbSet<AnimalBrand> AnimalBrands { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<FarmMember> FarmMembers { get; set; }
    public DbSet<ClinicalCase> ClinicalCases { get; set; }
    public DbSet<ClinicalCaseEvent> ClinicalCaseEvents { get; set; }
    public DbSet<ClinicalRecommendation> ClinicalRecommendations { get; set; }
    public DbSet<ClinicalAlert> ClinicalAlerts { get; set; }
    public DbSet<Medication> Medications { get; set; }
    public DbSet<MedicationRule> MedicationRules { get; set; }
    public DbSet<MedicationImage> MedicationImages { get; set; }
    public DbSet<TelegramInboundEventLog> TelegramInboundEventLogs { get; set; }
    public DbSet<TelegramOutboundMessage> TelegramOutboundMessages { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AgroLinkDbContext).Assembly);
    }
}
