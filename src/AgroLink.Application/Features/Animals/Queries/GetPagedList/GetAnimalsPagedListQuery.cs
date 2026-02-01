using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetPagedList;

public record GetAnimalsPagedListQuery(
    int FarmId,
    int Page = 1,
    int PageSize = 10,
    int? LotId = null,
    string? SearchTerm = null,
    bool IsSick = false,
    bool IsPregnant = false,
    bool IsMissing = false
) : IRequest<PagedResult<AnimalListDto>>;
