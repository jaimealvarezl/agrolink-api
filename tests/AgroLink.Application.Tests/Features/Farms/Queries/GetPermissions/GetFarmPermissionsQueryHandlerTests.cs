using AgroLink.Application.Features.Farms.Queries.GetPermissions;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using Moq.AutoMock;
using Shouldly;

namespace AgroLink.Application.Tests.Features.Farms.Queries.GetPermissions;

[TestFixture]
public class GetFarmPermissionsQueryHandlerTests
{
    [SetUp]
    public void Setup()
    {
        _mocker = new AutoMocker();
        _handler = _mocker.CreateInstance<GetFarmPermissionsQueryHandler>();
    }

    private AutoMocker _mocker = null!;
    private GetFarmPermissionsQueryHandler _handler = null!;

    private void SetRole(string? role)
    {
        _mocker.GetMock<ICurrentUserService>().Setup(s => s.CurrentFarmRole).Returns(role);
    }

    [Test]
    public async Task Handle_AsOwner_ReturnsAllPermissionsTrue()
    {
        SetRole(FarmMemberRoles.Owner);

        var result = await _handler.Handle(new GetFarmPermissionsQuery(), CancellationToken.None);

        result.CanCreateAnimal.ShouldBeTrue();
        result.CanUpdateAnimalBio.ShouldBeTrue();
        result.CanDeleteAnimal.ShouldBeTrue();
        result.CanLogOperations.ShouldBeTrue();
        result.CanViewChecklists.ShouldBeTrue();
        result.CanViewFinancials.ShouldBeTrue();
        result.CanUpdateFinancials.ShouldBeTrue();
        result.CanManageLocations.ShouldBeTrue();
        result.CanManagePartners.ShouldBeTrue();
        result.CanUpdateFarmMetadata.ShouldBeTrue();
        result.CanManageTeam.ShouldBeTrue();
        result.CanDeleteFarm.ShouldBeTrue();
    }

    [Test]
    public async Task Handle_AsAdmin_ReturnsAdminPermissions()
    {
        SetRole(FarmMemberRoles.Admin);

        var result = await _handler.Handle(new GetFarmPermissionsQuery(), CancellationToken.None);

        result.CanCreateAnimal.ShouldBeTrue();
        result.CanUpdateAnimalBio.ShouldBeTrue();
        result.CanDeleteAnimal.ShouldBeTrue();
        result.CanLogOperations.ShouldBeTrue();
        result.CanViewChecklists.ShouldBeTrue();
        result.CanViewFinancials.ShouldBeTrue();
        result.CanUpdateFinancials.ShouldBeTrue();
        result.CanManageLocations.ShouldBeTrue();
        result.CanManagePartners.ShouldBeTrue();
        result.CanUpdateFarmMetadata.ShouldBeFalse();
        result.CanManageTeam.ShouldBeFalse();
        result.CanDeleteFarm.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_AsEditor_ReturnsEditorPermissions()
    {
        SetRole(FarmMemberRoles.Editor);

        var result = await _handler.Handle(new GetFarmPermissionsQuery(), CancellationToken.None);

        result.CanCreateAnimal.ShouldBeTrue();
        result.CanUpdateAnimalBio.ShouldBeTrue();
        result.CanDeleteAnimal.ShouldBeFalse();
        result.CanLogOperations.ShouldBeTrue();
        result.CanViewChecklists.ShouldBeTrue();
        result.CanViewFinancials.ShouldBeFalse();
        result.CanUpdateFinancials.ShouldBeFalse();
        result.CanManageLocations.ShouldBeFalse();
        result.CanManagePartners.ShouldBeFalse();
        result.CanUpdateFarmMetadata.ShouldBeFalse();
        result.CanManageTeam.ShouldBeFalse();
        result.CanDeleteFarm.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_AsViewer_ReturnsViewerPermissions()
    {
        SetRole(FarmMemberRoles.Viewer);

        var result = await _handler.Handle(new GetFarmPermissionsQuery(), CancellationToken.None);

        result.CanViewChecklists.ShouldBeTrue();
        result.CanCreateAnimal.ShouldBeFalse();
        result.CanUpdateAnimalBio.ShouldBeFalse();
        result.CanDeleteAnimal.ShouldBeFalse();
        result.CanLogOperations.ShouldBeFalse();
        result.CanViewFinancials.ShouldBeFalse();
        result.CanUpdateFinancials.ShouldBeFalse();
        result.CanManageLocations.ShouldBeFalse();
        result.CanManagePartners.ShouldBeFalse();
        result.CanUpdateFarmMetadata.ShouldBeFalse();
        result.CanManageTeam.ShouldBeFalse();
        result.CanDeleteFarm.ShouldBeFalse();
    }

    [Test]
    public async Task Handle_NoRole_ReturnsAllPermissionsFalse()
    {
        SetRole(null);

        var result = await _handler.Handle(new GetFarmPermissionsQuery(), CancellationToken.None);

        result.CanViewChecklists.ShouldBeFalse();
        result.CanCreateAnimal.ShouldBeFalse();
        result.CanUpdateAnimalBio.ShouldBeFalse();
        result.CanDeleteAnimal.ShouldBeFalse();
        result.CanLogOperations.ShouldBeFalse();
        result.CanViewFinancials.ShouldBeFalse();
        result.CanUpdateFinancials.ShouldBeFalse();
        result.CanManageLocations.ShouldBeFalse();
        result.CanManagePartners.ShouldBeFalse();
        result.CanUpdateFarmMetadata.ShouldBeFalse();
        result.CanManageTeam.ShouldBeFalse();
        result.CanDeleteFarm.ShouldBeFalse();
    }
}
