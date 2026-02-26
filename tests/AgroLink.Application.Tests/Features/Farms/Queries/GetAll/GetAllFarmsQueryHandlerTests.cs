using System.Linq.Expressions;
using AgroLink.Application.Features.Farms.Queries.GetAll;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Queries.GetAll;

[TestFixture]
public class GetAllFarmsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetAllFarmsQueryHandler>();
    }

    private AutoMocker _mocker = null!;
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

        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(memberships);
        _mocker
            .GetMock<IFarmRepository>()
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
    public async Task Handle_ReturnsEmptyList_WhenNoAccessExists()
    {
        // Arrange
        var userId = 10;
        var query = new GetAllFarmsQuery(userId);
        _mocker
            .GetMock<IFarmMemberRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<FarmMember, bool>>>()))
            .ReturnsAsync(new List<FarmMember>());
        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Farm, bool>>>()))
            .ReturnsAsync(new List<Farm>());

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.ShouldBeEmpty();
    }
}
