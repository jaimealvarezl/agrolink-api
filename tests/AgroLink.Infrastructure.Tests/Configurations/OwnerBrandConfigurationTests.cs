using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Shouldly;

namespace AgroLink.Infrastructure.Tests.Configurations;

[TestFixture]
public class OwnerBrandConfigurationTests : TestBase
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

    [Test]
    public async Task OwnerBrand_ShouldPersistAllFields()
    {
        // Arrange
        var owner = await CreateTestOwnerAsync(_context);

        var brand = new OwnerBrand
        {
            OwnerId = owner.Id,
            RegistrationNumber = "REG-001",
            Description = "Tres rayas / Letra J",
            PhotoUrl = "https://storage/brand.jpg",
            IsActive = true,
            CreatedAt = DateTime.UtcNow,
        };

        _context.OwnerBrands.Add(brand);
        await _context.SaveChangesAsync();

        // Act
        _context.ChangeTracker.Clear();
        var result = await _context
            .OwnerBrands.IgnoreQueryFilters()
            .FirstAsync(b => b.Id == brand.Id);

        // Assert
        result.OwnerId.ShouldBe(owner.Id);
        result.RegistrationNumber.ShouldBe("REG-001");
        result.Description.ShouldBe("Tres rayas / Letra J");
        result.PhotoUrl.ShouldBe("https://storage/brand.jpg");
        result.IsActive.ShouldBeTrue();
    }

    [Test]
    public async Task OwnerBrand_PhotoUrl_ShouldBeNullable()
    {
        // Arrange
        var owner = await CreateTestOwnerAsync(_context);

        var brand = new OwnerBrand
        {
            OwnerId = owner.Id,
            RegistrationNumber = "REG-002",
            Description = "Cruz doble",
            PhotoUrl = null,
        };

        _context.OwnerBrands.Add(brand);
        await _context.SaveChangesAsync();

        _context.ChangeTracker.Clear();
        var result = await _context
            .OwnerBrands.IgnoreQueryFilters()
            .FirstAsync(b => b.Id == brand.Id);

        result.PhotoUrl.ShouldBeNull();
    }

    [Test]
    public async Task OwnerBrand_QueryFilter_ShouldExcludeInactiveOwnerBrands()
    {
        // Arrange
        var owner = await CreateTestOwnerAsync(_context);

        var activeBrand = new OwnerBrand
        {
            OwnerId = owner.Id,
            RegistrationNumber = "REG-ACTIVE",
            Description = "Active brand",
            IsActive = true,
        };
        var inactiveBrand = new OwnerBrand
        {
            OwnerId = owner.Id,
            RegistrationNumber = "REG-INACTIVE",
            Description = "Inactive brand",
            IsActive = false,
        };

        _context.OwnerBrands.AddRange(activeBrand, inactiveBrand);
        await _context.SaveChangesAsync();

        // Act
        var results = await _context.OwnerBrands.ToListAsync();

        // Assert
        results.Count.ShouldBe(1);
        results[0].RegistrationNumber.ShouldBe("REG-ACTIVE");
    }

    [Test]
    public async Task OwnerBrand_SameRegistrationNumber_AllowedForDifferentOwners()
    {
        // Arrange
        var owner1 = await CreateTestOwnerAsync(_context, "Owner One");
        var owner2 = await CreateTestOwnerAsync(_context, "Owner Two");

        _context.OwnerBrands.AddRange(
            new OwnerBrand
            {
                OwnerId = owner1.Id,
                RegistrationNumber = "REG-SHARED",
                Description = "Brand A",
            },
            new OwnerBrand
            {
                OwnerId = owner2.Id,
                RegistrationNumber = "REG-SHARED",
                Description = "Brand B",
            }
        );

        // Act & Assert — should not throw
        await Should.NotThrowAsync(() => _context.SaveChangesAsync());
    }
}
