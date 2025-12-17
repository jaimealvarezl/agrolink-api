using AgroLink.Application.Features.Auth.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Auth.Commands.Register;

public record RegisterCommand(RegisterRequest Request) : IRequest<AuthResponseDto>;
