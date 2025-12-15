using AgroLink.Application.Interfaces;

namespace AgroLink.Application.Services;

public class TokenExtractionService : ITokenExtractionService
{
    public string? ExtractTokenFromHeader(string authorizationHeader)
    {
        if (string.IsNullOrEmpty(authorizationHeader))
        {
            return null;
        }

        if (authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return authorizationHeader["Bearer ".Length..].Trim();
        }

        return authorizationHeader.Equals("Bearer", StringComparison.OrdinalIgnoreCase)
            ? string.Empty
            : null;
    }
}
