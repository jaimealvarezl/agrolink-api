using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroLink.Application.DTOs;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroLink.Application.Features.Auth.Commands.Register;

public class RegisterCommandHandler(AgroLinkDbContext context, IConfiguration configuration)
    : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    public async Task<AuthResponseDto> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken
    )
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(
            u => u.Email == request.Request.Email,
            cancellationToken
        );
        if (existingUser != null)
        {
            throw new ArgumentException("User with this email already exists");
        }

        var user = new User
        {
            Name = request.Request.Name,
            Email = request.Request.Email,
            PasswordHash = HashPassword(request.Request.Password),
            Role = request.Request.Role ?? "USER",
            IsActive = true,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);

        var registeredUserDto = new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
        };

        var token = GenerateJwtToken(registeredUserDto);

        return new AuthResponseDto
        {
            Token = token,
            User = registeredUserDto,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };
    }

    private string GenerateJwtToken(UserDto user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? "default-key");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity([
                new Claim("userid", user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim("email", user.Email),
                new Claim("role", user.Role),
                new Claim("name", user.Name),
            ]),
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string HashPassword(string password)
    {
        return BCrypt.Net.BCrypt.HashPassword(password);
    }
}
