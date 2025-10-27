namespace AgroLink.Core.Interfaces;

public interface ITokenExtractionService
{
    string? ExtractTokenFromHeader(string authorizationHeader);
}
