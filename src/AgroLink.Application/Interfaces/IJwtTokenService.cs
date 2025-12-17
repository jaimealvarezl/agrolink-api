using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;

namespace AgroLink.Application.Interfaces;

public interface IJwtTokenService
{
    string GenerateToken(User user);
    string GenerateToken(UserDto user);
    bool ValidateToken(string token);
    UserDto? GetUserFromToken(string token);
}
