using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using MediatR;

// For IAuthRepository, IJwtTokenService, IPasswordHasher

namespace AgroLink.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(
    IAuthRepository authRepository,
    IJwtTokenService jwtTokenService,
    IPasswordHasher passwordHasher
) : IRequestHandler<LoginCommand, AuthResponseDto?>
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

        if (!passwordHasher.VerifyPassword(request.LoginDto.Password, user.PasswordHash))
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
