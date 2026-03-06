using System.Linq.Expressions;
using AgroLink.Application.Features.Owners.Queries.GetByFarm;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Owners.Queries.GetByFarm;

[TestFixture]
public class GetOwnersByFarmIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetOwnersByFarmIdQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetOwnersByFarmIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ValidRequest_ReturnsActiveOwners()
    {
        // Arrange
        var query = new GetOwnersByFarmIdQuery(1);
        var owners = new List<Owner>
        {
            new()
            {
                Id = 1,
                Name = "Owner 1",
                IsActive = true,
                FarmId = 1,
            },
            new()
            {
                Id = 2,
                Name = "Owner 2",
                IsActive = true,
                FarmId = 1,
            },
        };

        _mocker
            .GetMock<IOwnerRepository>()
            .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Owner, bool>>>()))
            .ReturnsAsync(owners);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.ShouldNotBeNull();
        result.Count().ShouldBe(2);
        result.ShouldContain(o => o.Name == "Owner 1");
        result.ShouldContain(o => o.Name == "Owner 2");
    }
}
