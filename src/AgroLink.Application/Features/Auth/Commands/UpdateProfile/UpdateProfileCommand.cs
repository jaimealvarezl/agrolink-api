using AgroLink.Application.Features.Auth.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Auth.Commands.UpdateProfile;

public record UpdateProfileCommand(UpdateProfileRequest Request) : IRequest<UserDto>;
