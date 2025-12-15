using AgroLink.Application.DTOs;
using AgroLink.Application.Interfaces; // For IAuthRepository and IJwtTokenService
using MediatR;

namespace AgroLink.Application.Features.Auth.Queries.GetUserProfile;

public class GetUserProfileQueryHandler(
    IAuthRepository authRepository,
    IJwtTokenService jwtTokenService
) : IRequestHandler<GetUserProfileQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var userDtoFromToken = jwtTokenService.GetUserFromToken(request.Token);
            if (userDtoFromToken == null)
            {
                return null;
            }

            var user = await authRepository.GetUserByIdAsync(userDtoFromToken.Id);
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
