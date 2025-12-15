using AgroLink.Application.DTOs;

namespace AgroLink.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponseDto?> LoginAsync(LoginDto dto);
    Task<UserDto> RegisterAsync(UserDto dto, string password);
    Task<bool> ValidateTokenAsync(string token);
    Task<UserDto?> GetUserFromTokenAsync(string token);

    // New methods for controller logic
    Task<AuthResponseDto> RegisterUserAsync(RegisterRequest request);
    Task<UserDto?> GetUserProfileAsync(string token);
    Task<ValidateTokenResponse> ValidateTokenResponseAsync(string token);
}
