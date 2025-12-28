using AgroLink.Application.Interfaces;

namespace AgroLink.Api.Services;

public class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public int? UserId
    {
        get
        {
            var userIdClaim = httpContextAccessor.HttpContext?.User?.FindFirst("userid");
            return userIdClaim != null && int.TryParse(userIdClaim.Value, out var userId)
                ? userId
                : null;
        }
    }

    public string? Email => httpContextAccessor.HttpContext?.User?.FindFirst("email")?.Value;

    public string? Role => httpContextAccessor.HttpContext?.User?.FindFirst("role")?.Value;

    public int GetRequiredUserId()
    {
        return UserId ?? throw new UnauthorizedAccessException("User is not authenticated");
    }
}
