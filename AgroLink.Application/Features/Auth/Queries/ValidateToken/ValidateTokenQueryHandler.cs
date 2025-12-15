using System.IdentityModel.Tokens.Jwt;
using System.Text;
using AgroLink.Application.DTOs;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace AgroLink.Application.Features.Auth.Queries.ValidateToken;

public class ValidateTokenQueryHandler(IConfiguration configuration)
    : IRequestHandler<ValidateTokenQuery, ValidateTokenResponse>
{
    public async Task<ValidateTokenResponse> Handle(
        ValidateTokenQuery request,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            return new ValidateTokenResponse { Valid = false };
        }

        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();

            // Check if token can be read (basic format validation)
            if (!tokenHandler.CanReadToken(request.Token))
            {
                return new ValidateTokenResponse { Valid = false };
            }

            // Try to read the token to ensure it's properly formatted
            try
            {
                tokenHandler.ReadJwtToken(request.Token);
            }
            catch
            {
                return new ValidateTokenResponse { Valid = false };
            }

            var key = Encoding.ASCII.GetBytes(configuration["Jwt:Key"] ?? "default-key");

            var result = await tokenHandler.ValidateTokenAsync(
                request.Token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidateAudience = true,
                    ValidAudience = configuration["Jwt:Audience"],
                    ClockSkew = TimeSpan.Zero,
                }
            );

            return new ValidateTokenResponse { Valid = result != null };
        }
        catch (SecurityTokenException)
        {
            return new ValidateTokenResponse { Valid = false };
        }
        catch
        {
            return new ValidateTokenResponse { Valid = false };
        }
    }
}
