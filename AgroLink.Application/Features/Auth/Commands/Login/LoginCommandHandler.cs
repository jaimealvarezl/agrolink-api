using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using AgroLink.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroLink.Application.Features.Auth.Commands.Login;

public class LoginCommandHandler(AgroLinkDbContext context, IConfiguration configuration)
    : IRequestHandler<LoginCommand, AuthResponseDto?>
{
    public async Task<AuthResponseDto?> Handle(
        LoginCommand request,
        CancellationToken cancellationToken
    )
    {
        var user = await context.Users.FirstOrDefaultAsync(
            u => u.Email == request.LoginDto.Email && u.IsActive,
            cancellationToken
        );
        if (user == null)
        {
            return null;
        }

        if (!VerifyPassword(request.LoginDto.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);

        var token = GenerateJwtToken(user);
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

    private string GenerateJwtToken(User user)
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

    private static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
