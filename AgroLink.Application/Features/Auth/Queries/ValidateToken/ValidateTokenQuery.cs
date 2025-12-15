using AgroLink.Application.DTOs;
using MediatR;

namespace AgroLink.Application.Features.Auth.Queries.ValidateToken;

public record ValidateTokenQuery(string Token) : IRequest<ValidateTokenResponse>;
