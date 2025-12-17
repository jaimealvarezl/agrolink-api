using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Auth.Commands.Login;

public record LoginCommand(LoginDto LoginDto) : IRequest<AuthResponseDto?>;
