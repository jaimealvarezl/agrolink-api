using AgroLink.Application.Features.Auth.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginDto LoginDto) : IRequest<AuthResponseDto?>;
