using System.Globalization;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroLink.Infrastructure.Services;

public class JwtTokenService(IConfiguration configuration) : IJwtTokenService
{
    private readonly string _jwtKey = configuration["Jwt:Key"] ?? "default-key";
    private readonly string _jwtIssuer = configuration["Jwt:Issuer"] ?? "default-issuer";
    private readonly string _jwtAudience = configuration["Jwt:Audience"] ?? "default-audience";

    public string GenerateToken(User user)
    {
        return GenerateJwtTokenInternal(
            new ClaimsIdentity([
                new Claim("userid", user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim("email", user.Email),
                new Claim("role", user.Role),
                new Claim("name", user.Name),
            ])
        );
    }

    public string GenerateToken(UserDto user)
    {
        return GenerateJwtTokenInternal(
            new ClaimsIdentity([
                new Claim("userid", user.Id.ToString(CultureInfo.InvariantCulture)),
                new Claim("email", user.Email),
                new Claim("role", user.Role),
                new Claim("name", user.Name),
            ])
        );
    }

    public bool ValidateToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return false;
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Check if token can be read (basic format validation)
            if (!tokenHandler.CanReadToken(token))
            {
                return false;
            }

            // Try to read the token to ensure it's properly formatted
            try
            {
                tokenHandler.ReadJwtToken(token);
            }
            catch
            {
                return false;
            }

            var key = Encoding.ASCII.GetBytes(_jwtKey);

            var result = tokenHandler.ValidateToken(
                token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = _jwtIssuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtAudience,
                    ClockSkew = TimeSpan.Zero,
                },
                out SecurityToken validatedToken
            );

            return result != null;
        }
        catch (SecurityTokenException)
        {
            return false;
        }
        catch
        {
            return false;
        }
    }

    public UserDto? GetUserFromToken(string token)
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

            // In a real application, you might fetch the user from the database
            // here to ensure the user is still active and valid.
            // For this example, we'll just reconstruct the DTO from token claims.
            return new UserDto
            {
                Id = userId,
                Name = jwt.Claims.FirstOrDefault(x => x.Type == "name")?.Value ?? "",
                Email = jwt.Claims.FirstOrDefault(x => x.Type == "email")?.Value ?? "",
                Role = jwt.Claims.FirstOrDefault(x => x.Type == "role")?.Value ?? "",
                IsActive = true, // Assuming active if token is valid
                CreatedAt = DateTime.MinValue, // Not available in token
                LastLoginAt = DateTime.MinValue, // Not available in token
            };
        }
        catch
        {
            return null;
        }
    }

    private string GenerateJwtTokenInternal(ClaimsIdentity claimsIdentity)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_jwtKey);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = claimsIdentity,
            Expires = DateTime.UtcNow.AddDays(7),
            Issuer = _jwtIssuer,
            Audience = _jwtAudience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature
            ),
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }
}
