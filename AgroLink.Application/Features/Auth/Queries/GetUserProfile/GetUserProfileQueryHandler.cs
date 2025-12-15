using System.IdentityModel.Tokens.Jwt;
using AgroLink.Application.DTOs;
using AgroLink.Infrastructure.Data;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace AgroLink.Application.Features.Auth.Queries.GetUserProfile;

public class GetUserProfileQueryHandler(AgroLinkDbContext context)
    : IRequestHandler<GetUserProfileQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            var jwt = tokenHandler.ReadJwtToken(request.Token);

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
}
