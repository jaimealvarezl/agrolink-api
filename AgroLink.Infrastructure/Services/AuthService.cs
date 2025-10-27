using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroLink.Core.DTOs;
using AgroLink.Core.Entities;
using AgroLink.Core.Interfaces;
using AgroLink.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroLink.Infrastructure.Services;

public class AuthService(AgroLinkDbContext context, IConfiguration configuration) : IAuthService
{
    public async Task<AuthResponseDto?> LoginAsync(LoginDto dto)
    {
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email && u.IsActive);
        if (user == null)
        {
            return null;
        }

        if (!VerifyPassword(dto.Password, user.PasswordHash))
        {
            return null;
        }

        user.LastLoginAt = DateTime.UtcNow;
        context.Users.Update(user);
        await context.SaveChangesAsync();

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

    public async Task<UserDto> RegisterAsync(UserDto dto, string password)
    {
        var existingUser = await context.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existingUser != null)
        {
            throw new ArgumentException("User with this email already exists");
        }

        var user = new User
        {
            Name = dto.Name,
            Email = dto.Email,
            PasswordHash = HashPassword(password),
            Role = dto.Role,
            IsActive = true,
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();

        return new UserDto
        {
            Id = user.Id,
            Name = user.Name,
            Email = user.Email,
            Role = user.Role,
            IsActive = user.IsActive,
            CreatedAt = user.CreatedAt,
        };
    }

    public async Task<bool> ValidateTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? "default-key");

            tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero,
                },
                out _
            );

            return true;
        }
        catch
        {
            return false;
        }
    }

    public async Task<UserDto?> GetUserFromTokenAsync(string token)
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(token);

            var userIdClaim = jwt.Claims.FirstOrDefault(x => x.Type == "userid");
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
            {
                return null;
            }

            var user = await context.Users.FindAsync(userId);
            if (user == null || !user.IsActive)
            {
                return null;
            }

            return new UserDto
            {
                Id = user.Id,
                Name = user.Name,
                Email = user.Email,
                Role = user.Role,
                IsActive = user.IsActive,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
            };
        }
        catch
        {
            return null;
        }
    }

    // New methods for controller logic
    public async Task<UserDto> RegisterUserAsync(RegisterRequest request)
    {
        var userDto = new UserDto
        {
            Name = request.Name,
            Email = request.Email,
            Role = request.Role ?? "USER",
        };

        return await RegisterAsync(userDto, request.Password);
    }

    public async Task<UserDto?> GetUserProfileAsync(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            return null;
        }

        return await GetUserFromTokenAsync(token);
    }

    public async Task<ValidateTokenResponse> ValidateTokenResponseAsync(string token)
    {
        var isValid = await ValidateTokenAsync(token);
        return new ValidateTokenResponse { Valid = isValid };
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? "default-key");

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(
                [
                    new Claim("userid", user.Id.ToString()),
                    new Claim("email", user.Email),
                    new Claim("role", user.Role),
                    new Claim("name", user.Name),
                ]
            ),
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

    private static bool VerifyPassword(string password, string hash)
    {
        return BCrypt.Net.BCrypt.Verify(password, hash);
    }
}
