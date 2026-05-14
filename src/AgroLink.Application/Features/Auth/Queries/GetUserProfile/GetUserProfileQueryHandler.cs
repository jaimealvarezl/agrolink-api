using AgroLink.Application.Features.Auth.DTOs;
using AgroLink.Application.Interfaces;
using MediatR;

namespace AgroLink.Application.Features.Auth.Queries.GetUserProfile;

public class GetUserProfileQueryHandler(
    IAuthRepository authRepository,
    ICurrentUserService currentUserService
) : IRequestHandler<GetUserProfileQuery, UserDto?>
{
    public async Task<UserDto?> Handle(
        GetUserProfileQuery request,
        CancellationToken cancellationToken
    )
    {
        var userId = currentUserService.UserId;
        if (userId == null)
        {
            return null;
        }

        var user = await authRepository.GetUserByIdAsync(userId.Value, cancellationToken);
        if (user is not { IsActive: true })
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
}
