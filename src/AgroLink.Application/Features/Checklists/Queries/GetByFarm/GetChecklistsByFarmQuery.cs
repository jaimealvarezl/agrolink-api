using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Checklists.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Checklists.Queries.GetByFarm;

public record GetChecklistsByFarmQuery(
    int FarmId,
    int Page = 1,
    int PageSize = 20
) : IRequest<PagedResult<ChecklistDto>>;
