using AgroLink.Application.Features.Farms.Queries.GetById;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Entities;
using AgroLink.Domain.Interfaces;
using Moq;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Queries.GetById;

[TestFixture]
public class GetFarmByIdQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetFarmByIdQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetFarmByIdQueryHandler _handler = null!;

    [Test]
    public async Task Handle_ExistingFarm_ReturnsFarmDtoWithCurrentRole()
    {
        var farmId = 1;
        var query = new GetFarmByIdQuery(farmId, 10);
        var farm = new Farm
        {
            Id = farmId,
            Name = "Test Farm",
            Location = "Test Location",
            CreatedAt = DateTime.UtcNow,
        };

        _mocker.GetMock<IFarmRepository>().Setup(r => r.GetByIdAsync(farmId)).ReturnsAsync(farm);
        _mocker
            .GetMock<ICurrentUserService>()
            .Setup(s => s.CurrentFarmRole)
            .Returns(FarmMemberRoles.Owner);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ShouldNotBeNull();
        result.Id.ShouldBe(farmId);
        result.Name.ShouldBe(farm.Name);
        result.Role.ShouldBe(FarmMemberRoles.Owner);
    }

    [Test]
    public async Task Handle_NonExistingFarm_ReturnsNull()
    {
        var query = new GetFarmByIdQuery(999, 10);

        _mocker
            .GetMock<IFarmRepository>()
            .Setup(r => r.GetByIdAsync(999))
            .ReturnsAsync((Farm?)null);

        var result = await _handler.Handle(query, CancellationToken.None);

        result.ShouldBeNull();
    }
}
