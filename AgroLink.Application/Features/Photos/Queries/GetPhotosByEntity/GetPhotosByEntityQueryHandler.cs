using AgroLink.Application.DTOs;
using AgroLink.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Application.Features.Photos.Queries.GetPhotosByEntity;

public class GetPhotosByEntityQueryHandler(AgroLinkDbContext context)
    : IRequestHandler<GetPhotosByEntityQuery, IEnumerable<PhotoDto>>
{
    public async Task<IEnumerable<PhotoDto>> Handle(
        GetPhotosByEntityQuery request,
        CancellationToken cancellationToken
    )
    {
        var photos = await context
            .Photos.Where(p => p.EntityType == request.EntityType && p.EntityId == request.EntityId)
            .ToListAsync(cancellationToken);

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
