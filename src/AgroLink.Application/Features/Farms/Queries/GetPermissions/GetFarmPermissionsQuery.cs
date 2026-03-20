using AgroLink.Application.Features.Farms.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using MediatR;

namespace AgroLink.Application.Features.Farms.Queries.GetPermissions;

public record GetFarmPermissionsQuery : IRequest<FarmPermissionsDto>;

public class GetFarmPermissionsQueryHandler(ICurrentUserService currentUserService)
    : IRequestHandler<GetFarmPermissionsQuery, FarmPermissionsDto>
{
    public Task<FarmPermissionsDto> Handle(
        GetFarmPermissionsQuery request,
        CancellationToken cancellationToken
    )
    {
        var role = currentUserService.CurrentFarmRole;

        var isOwner = role == FarmMemberRoles.Owner;
        var isAdmin = isOwner || role == FarmMemberRoles.Admin;
        var isEditor = isAdmin || role == FarmMemberRoles.Editor;
        var isViewer = isEditor || role == FarmMemberRoles.Viewer;

        var permissions = new FarmPermissionsDto
        {
            CanCreateAnimal = isEditor,
            CanUpdateAnimalBio = isEditor,
            CanDeleteAnimal = isAdmin,
            CanViewAnimalNotes = isViewer,
            CanCreateAnimalNote = isEditor,
            CanViewChecklists = isViewer,
            CanCreateChecklist = isEditor,
            CanViewFinancials = isAdmin,
            CanUpdateFinancials = isAdmin,
            CanManageLocations = isAdmin,
            CanManagePartners = isAdmin,
            CanUpdateFarmMetadata = isOwner,
            CanManageTeam = isOwner,
            CanDeleteFarm = isOwner,
        };

        return Task.FromResult(permissions);
    }
}
