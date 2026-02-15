using AgroLink.Application.Common.Exceptions;
using AgroLink.Application.Common.Utilities;
using AgroLink.Application.Features.Animals.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Enums;
using AgroLink.Domain.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Animals.Queries.GetPagedList;

public class GetAnimalsPagedListQueryHandler(
    IAnimalRepository animalRepository,
    IFarmMemberRepository farmMemberRepository,
    ICurrentUserService currentUserService,
    IStorageService storageService
) : IRequestHandler<GetAnimalsPagedListQuery, PagedResult<AnimalListDto>>
{
    public async Task<PagedResult<AnimalListDto>> Handle(
        GetAnimalsPagedListQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.GetRequiredUserId();
        var isMember = await farmMemberRepository.ExistsAsync(fm =>
            fm.UserId == userId && fm.FarmId == request.FarmId
        );

        if (!isMember)
        {
            throw new ForbiddenAccessException("You do not have access to this farm's animals.");
        }

        var (items, totalCount) = await animalRepository.GetPagedListAsync(
            request.FarmId,
            request.Page,
            request.PageSize,
            request.LotId,
            request.SearchTerm,
            request.IsSick,
            request.IsPregnant,
            request.IsMissing,
            request.Sex
        );

        var dtos = items.Select(a =>
        {
            var primaryPhoto =
                a.Photos.FirstOrDefault(p => p.IsProfile) ?? a.Photos.FirstOrDefault();
            var photoUrl =
                primaryPhoto != null
                    ? storageService.GetPresignedUrl(primaryPhoto.StorageKey, TimeSpan.FromHours(1))
                    : null;

            return new AnimalListDto
            {
                Id = a.Id,
                TagVisual = a.TagVisual,
                Name = a.Name,
                PhotoUrl = photoUrl,
                LotName = a.Lot.Name,
                IsSick = a.HealthStatus == HealthStatus.Sick,
                IsPregnant = a.ReproductiveStatus == ReproductiveStatus.Pregnant,
                IsMissing = a.LifeStatus == LifeStatus.Missing,
            };
        });

        return new PagedResult<AnimalListDto>(dtos, totalCount, request.Page, request.PageSize);
    }
}
