using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Photos.Commands.UploadPhoto;

public record UploadPhotoCommand(CreatePhotoDto PhotoDto, Stream FileStream, string FileName)
    : IRequest<PhotoDto>;
