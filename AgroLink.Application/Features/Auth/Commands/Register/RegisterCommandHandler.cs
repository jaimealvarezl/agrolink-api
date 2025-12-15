using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces; // For IAuthRepository and IJwtTokenService
using AgroLink.Domain.Entities;
using BCrypt.Net; // For HashPassword
using MediatR;

namespace AgroLink.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(
    IAuthRepository authRepository,
    IJwtTokenService jwtTokenService
) : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken
    )
    {
        var existingUser = await authRepository.GetUserByEmailAsync(request.Request.Email);
        if (existingUser != null)
        {
            throw new ArgumentException("User with this email already exists");
        }

        var user = new User
        {
            Name = request.Request.Name,
            Email = request.Request.Email,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Request.Password),
            Role = request.Request.Role ?? "USER",
            IsActive = true,
        };

        await authRepository.AddUserAsync(user);

        var registeredUserDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
        };

        var token = jwtTokenService.GenerateToken(registeredUserDto);

        return new AuthResponseDto
        {
            Token = token,
            User = registeredUserDto,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
    }
}
