namespace AgroLink.Application.Interfaces;

public interface ITokenExtractionService
{
    string? ExtractTokenFromHeader(string authorizationHeader);
}
