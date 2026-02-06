using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetPagedList;

public class GetAnimalsPagedListQueryHandler(IAnimalRepository animalRepository)
    : IRequestHandler<GetAnimalsPagedListQuery, PagedResult<AnimalListDto>>
{
    public async Task<PagedResult<AnimalListDto>> Handle(
        GetAnimalsPagedListQuery request,
        CancellationToken cancellationToken
    )
    {
        var (items, totalCount) = await animalRepository.GetPagedListAsync(
            request.FarmId,
            request.Page,
            request.PageSize,
            request.LotId,
            request.SearchTerm,
            request.IsSick,
            request.IsPregnant,
            request.IsMissing
        );

        var dtos = items.Select(a => new AnimalListDto
        {
            Id = a.Id,
            TagVisual = a.TagVisual,
            Name = a.Name,
            PhotoUrl = a.Photos.FirstOrDefault()?.UriRemote,
            LotName = a.Lot.Name,
            IsSick = a.HealthStatus == HealthStatus.Sick,
            IsPregnant = a.ReproductiveStatus == ReproductiveStatus.Pregnant,
            IsMissing = a.LifeStatus == LifeStatus.Missing,
        });

        return new PagedResult<AnimalListDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
