using System.Linq.Expressions;
using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Features.OwnerBrands.Queries.GetByOwner;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.OwnerBrands.Queries.GetByOwner;

[TestFixture]
public class GetOwnerBrandsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetOwnerBrandsQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetOwnerBrandsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_ReturnsBrandsForOwner()
    {
        // Arrange
        var query = new GetOwnerBrandsQuery(1, 10);
        var brands = new List<OwnerBrand>
        {
            new()
            {
                Id = 1,
                OwnerId = 10,
                Description = "Brand 1",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
            new()
            {
                Id = 2,
                OwnerId = 10,
                Description = "Brand 2",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            },
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(true);

        _mocker
            .GetMock<IOwnerBrandRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<OwnerBrand, bool>>>()))
            .ReturnsAsync(brands);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.ShouldContain(b => b.Description == "Brand 1");
        result.ShouldContain(b => b.Description == "Brand 2");
    }

    [Test]
    public async Task Handle_OwnerNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var query = new GetOwnerBrandsQuery(1, 99);

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.ExistsAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(false);

        // Act & Assert
        await Should.ThrowAsync<NotFoundException>(() =>
            _handler.Handle(query, CancellationToken.None)
        );
    }
}
