using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Queries.GetById;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Queries.GetById;

[TestFixture]
public class GetFarmByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _handler = new GetFarmByIdQueryHandler(
            _farmRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _ownerRepositoryMock.Object
        );
    }

    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private GetFarmByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingFarmWithAccess_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var query = new GetFarmByIdQuery(farmId, userId);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Test Farm",
            Location = "Test Location",
            CreatedAt = DateTime.UtcNow,
        };
        var membership = new FarmMember
        {
            FarmId = farmId,
            UserId = userId,
            Role = FarmMemberRoles.Owner,
        };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(membership);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(farm.Name);
        result.Role.ShouldBe(FarmMemberRoles.Owner);
    }

    [Test]
    public async Task Handle_ExistingFarmWithAccessViaOwnerTable_ReturnsFarmDto()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var ownerId = 20;
        var query = new GetFarmByIdQuery(farmId, userId);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Test Farm",
            OwnerId = ownerId,
        };
        var owner = new Owner { Id = ownerId, UserId = userId };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync((FarmMember?)null);
        _ownerRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owner);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Role.ShouldBe(FarmMemberRoles.Owner);
    }

    [Test]
    public async Task Handle_ExistingFarmWithoutAccess_ReturnsNull()
    {
        // Arrange
        var farmId = 1;
        var userId = 10;
        var query = new GetFarmByIdQuery(farmId, userId);
        var farm = new Farm { Id = farmId, Name = "Test Farm" };

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _farmMemberRepositoryMock
            .Setup(r => r.FirstOrDefaultAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync((FarmMember?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }

    [Test]
    public async Task Handle_NonExistingFarm_ReturnsNull()
    {
        // Arrange
        var farmId = 999;
        var userId = 10;
        var query = new GetFarmByIdQuery(farmId, userId);

        _farmRepositoryMock.Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync((Farm?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldBeNull();
    }
}
