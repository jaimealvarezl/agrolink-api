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
    public async Task OwnerBrand_QueryFilter_ShouldExcludeInactiveBrands()
    {
        // Arrange
        var owner = await CreateTestOwnerAsync(_context);

        _context.OwnerBrands.AddRange(
            new OwnerBrand
            {
                OwnerId = owner.Id,
                Description = "Active brand",
                IsActive = true,
            },
            new OwnerBrand
            {
                OwnerId = owner.Id,
                Description = "Inactive brand",
                IsActive = false,
            }
        );
        await _context.SaveChangesAsync();

        // Act
        var results = await _context.OwnerBrands.ToListAsync();

        // Assert
        results.Count.ShouldBe(1);
        results[0].Description.ShouldBe("Active brand");
    }
}
