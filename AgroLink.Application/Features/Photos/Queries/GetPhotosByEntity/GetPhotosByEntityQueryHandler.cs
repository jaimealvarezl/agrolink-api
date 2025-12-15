using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces; // For IPhotoRepository
using MediatR;

namespace AgroLink.Application.Features.Photos.Queries.GetPhotosByEntity;

public class GetPhotosByEntityQueryHandler(IPhotoRepository photoRepository)
    : IRequestHandler<GetPhotosByEntityQuery, IEnumerable<PhotoDto>>
{
    public async Task<IEnumerable<PhotoDto>> Handle(
        GetPhotosByEntityQuery request,
        CancellationToken cancellationToken
    )
    {
        var photos = await photoRepository.GetPhotosByEntityAsync(
            request.EntityType,
            request.EntityId
        );

        return photos
            .Select(p => new PhotoDto
            {
                Id = p.Id,
                EntityType = p.EntityType,
                EntityId = p.EntityId,
                UriLocal = p.UriLocal,
                UriRemote = p.UriRemote,
                Uploaded = p.Uploaded,
                Description = p.Description,
                CreatedAt = p.CreatedAt,
            })
            .ToList();
    }
}
