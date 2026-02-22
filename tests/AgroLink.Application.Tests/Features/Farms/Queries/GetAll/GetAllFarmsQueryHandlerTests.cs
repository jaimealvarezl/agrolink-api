using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Queries.GetAll;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Queries.GetAll;

[TestFixture]
public class GetAllFarmsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _farmRepositoryMock = new Mock<IFarmRepository>();
        _farmMemberRepositoryMock = new Mock<IFarmMemberRepository>();
        _ownerRepositoryMock = new Mock<IOwnerRepository>();
        _handler = new GetAllFarmsQueryHandler(
            _farmRepositoryMock.Object,
            _farmMemberRepositoryMock.Object,
            _ownerRepositoryMock.Object
        );
    }

    private Mock<IFarmRepository> _farmRepositoryMock = null!;
    private Mock<IFarmMemberRepository> _farmMemberRepositoryMock = null!;
    private Mock<IOwnerRepository> _ownerRepositoryMock = null!;
    private GetAllFarmsQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ReturnsOnlyAccessibleFarms()
    {
        // Arrange
        var userId = 10;
        var query = new GetAllFarmsQuery(userId);
        var memberships = new List<FarmMember>
        {
            new()
            {
                FarmId = 1,
                UserId = userId,
                Role = FarmMemberRoles.Owner,
            },
        };
        var farms = new List<Farm>
        {
            new()
            {
                Id = 1,
                Name = "Farm 1",
                Location = "Location 1",
                CreatedAt = DateTime.UtcNow,
            },
        };

        _farmMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(memberships);
        _ownerRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(new List<Owner>());
        _farmRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(farms);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("Farm 1");
        result.First().Role.ShouldBe(FarmMemberRoles.Owner);
    }

    [Test]
    public async Task Handle_ReturnsFarmsWhereUserIsOwnerViaTable()
    {
        // Arrange
        var userId = 10;
        var ownerId = 20;
        var query = new GetAllFarmsQuery(userId);
        var owners = new List<Owner>
        {
            new() { Id = ownerId, UserId = userId },
        };
        var farms = new List<Farm>
        {
            new()
            {
                Id = 1,
                Name = "Owned Farm",
                OwnerId = ownerId,
            },
        };

        _farmMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(new List<FarmMember>());
        _ownerRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owners);
        _farmRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(farms);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(1);
        result.First().Name.ShouldBe("Owned Farm");
        result.First().Role.ShouldBe(FarmMemberRoles.Owner);
    }

    [Test]
    public async Task Handle_ReturnsEmptyList_WhenNoAccessExists()
    {
        // Arrange
        var userId = 10;
        var query = new GetAllFarmsQuery(userId);
        _farmMemberRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(new List<FarmMember>());
        _ownerRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(new List<Owner>());
        _farmRepositoryMock
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(new List<Farm>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
