using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Auth.Queries.ValidateToken;

public class ValidateTokenQueryHandler(IJwtTokenService jwtTokenService)
    : IRequestHandler<ValidateTokenQuery, ValidateTokenResponse>
{
    public Task<ValidateTokenResponse> Handle( // Removed async
        ValidateTokenQuery request,
        CancellationToken cancellationToken
    )
    {
        var isValid = jwtTokenService.ValidateToken(request.Token);
        return Task.FromResult(new ValidateTokenResponse { Valid = isValid }); // Changed to return Task.FromResult
    }
}
