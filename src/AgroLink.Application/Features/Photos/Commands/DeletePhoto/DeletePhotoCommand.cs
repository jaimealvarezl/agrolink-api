using MediatR;

namespace AgroLink.Application.Features.Photos.Commands.DeletePhoto;

public record DeletePhotoCommand(int Id) : IRequest<Unit>;
