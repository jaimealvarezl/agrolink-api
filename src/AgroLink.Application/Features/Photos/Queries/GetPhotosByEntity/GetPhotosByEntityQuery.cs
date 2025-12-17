using AgroLink.Application.Features.Photos.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Photos.Queries.GetPhotosByEntity;

public record GetPhotosByEntityQuery(string EntityType, int EntityId)
    : IRequest<IEnumerable<PhotoDto>>;
