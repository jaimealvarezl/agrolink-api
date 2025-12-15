using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces; // For IAuthRepository and IJwtTokenService
using BCrypt.Net; // For VerifyPassword
using MediatR;

namespace AgroLink.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(IAuthRepository authRepository, IJwtTokenService jwtTokenService)
    : IRequestHandler<LoginCommand, AuthResponseDto?>
{
    public async Task<AuthResponseDto?> Handle(
        LoginCommand request,
        CancellationToken cancellationToken
    )
    {
        var user = await authRepository.GetUserByEmailAsync(request.LoginDto.Email);
        if (user == null || !user.IsActive)
        {
            return null;
        }

        if (!BCrypt.Net.BCrypt.Verify(request.LoginDto.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        await authRepository.UpdateUserAsync(user);

        var token = jwtTokenService.GenerateToken(user);
        var expiresAt = DateTime.UtcNow.AddDays(7); // Token expires in 7 days

        return new AuthResponseDto
        {
            Token = token,
            User = new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
            },
            ExpiresAt = expiresAt,
        };
    }
}
