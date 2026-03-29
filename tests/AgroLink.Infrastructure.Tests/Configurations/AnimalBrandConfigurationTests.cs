using AgroLink.Domain.Entities;
using AgroLink.Domain.Enums;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Configurations;

[TestFixture]
public class AnimalBrandConfigurationTests : TestBase
{
    [SetUp]
    public void Setup()
    {
        _context = CreateInMemoryContext();
    }

    [TearDown]
    public void TearDown()
    {
        _context.Dispose();
    }

    private AgroLinkDbContext _context = null!;

    private async Task<OwnerBrand> CreateTestOwnerBrandAsync(
        AgroLinkDbContext context,
        int ownerId,
        string registrationNumber = "REG-001"
    )
    {
        var brand = new OwnerBrand
        {
            OwnerId = ownerId,
            RegistrationNumber = registrationNumber,
            Description = "Test Brand",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };
        context.OwnerBrands.Add(brand);
        await context.SaveChangesAsync();
        return brand;
    }

    [Test]
    public async Task AnimalBrand_ShouldPersistAllFields()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);
        var owner = await CreateTestOwnerAsync(_context);
        var ownerBrand = await CreateTestOwnerBrandAsync(_context, owner.Id);

        var animalBrand = new AnimalBrand
        {
            AnimalId = animal.Id,
            OwnerBrandId = ownerBrand.Id,
            AppliedAt = new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc),
            Notes = "Applied after purchase",
            CreatedAt = DateTime.UtcNow,
        };

        _context.AnimalBrands.Add(animalBrand);
        await _context.SaveChangesAsync();

        // Act
        _context.ChangeTracker.Clear();
        var result = await _context
            .AnimalBrands.IgnoreQueryFilters()
            .FirstAsync(ab => ab.Id == animalBrand.Id);

        // Assert
        result.AnimalId.ShouldBe(animal.Id);
        result.OwnerBrandId.ShouldBe(ownerBrand.Id);
        result.AppliedAt.ShouldBe(new DateTime(2025, 3, 1, 0, 0, 0, DateTimeKind.Utc));
        result.Notes.ShouldBe("Applied after purchase");
    }

    [Test]
    public async Task AnimalBrand_AppliedAtAndNotes_ShouldBeNullable()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);
        var owner = await CreateTestOwnerAsync(_context);
        var ownerBrand = await CreateTestOwnerBrandAsync(_context, owner.Id);

        var animalBrand = new AnimalBrand
        {
            AnimalId = animal.Id,
            OwnerBrandId = ownerBrand.Id,
            AppliedAt = null,
            Notes = null,
        };

        _context.AnimalBrands.Add(animalBrand);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
        var result = await _context
            .AnimalBrands.IgnoreQueryFilters()
            .FirstAsync(ab => ab.Id == animalBrand.Id);

        result.AppliedAt.ShouldBeNull();
        result.Notes.ShouldBeNull();
    }

    [Test]
    public async Task AnimalBrand_QueryFilter_ShouldExcludeDeletedAnimals()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var activeAnimal = await CreateTestAnimalAsync(_context, lot.Id);
        var deletedAnimal = await CreateTestAnimalAsync(_context, lot.Id, "A002");
        deletedAnimal.LifeStatus = LifeStatus.Deleted;
        await _context.SaveChangesAsync();

        var owner = await CreateTestOwnerAsync(_context);
        var ownerBrand = await CreateTestOwnerBrandAsync(_context, owner.Id);

        _context.AnimalBrands.AddRange(
            new AnimalBrand { AnimalId = activeAnimal.Id, OwnerBrandId = ownerBrand.Id },
            new AnimalBrand { AnimalId = deletedAnimal.Id, OwnerBrandId = ownerBrand.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var results = await _context.AnimalBrands.ToListAsync();

        // Assert
        results.Count.ShouldBe(1);
        results[0].AnimalId.ShouldBe(activeAnimal.Id);
    }

    [Test]
    public async Task AnimalBrand_AnimalCanHaveMultipleBrands()
    {
        // Arrange
        var farm = await CreateTestFarmAsync(_context);
        var paddock = await CreateTestPaddockAsync(_context, farm.Id);
        var lot = await CreateTestLotAsync(_context, paddock.Id);
        var animal = await CreateTestAnimalAsync(_context, lot.Id);

        var owner1 = await CreateTestOwnerAsync(_context, "Owner One");
        var owner2 = await CreateTestOwnerAsync(_context, "Owner Two");
        var brand1 = await CreateTestOwnerBrandAsync(_context, owner1.Id);
        var brand2 = await CreateTestOwnerBrandAsync(_context, owner2.Id, "REG-002");

        _context.AnimalBrands.AddRange(
            new AnimalBrand { AnimalId = animal.Id, OwnerBrandId = brand1.Id },
            new AnimalBrand { AnimalId = animal.Id, OwnerBrandId = brand2.Id }
        );
        await _context.SaveChangesAsync();

        // Act
        var results = await _context
            .AnimalBrands.IgnoreQueryFilters()
            .Where(ab => ab.AnimalId == animal.Id)
            .ToListAsync();

        // Assert
        results.Count.ShouldBe(2);
    }
}
