using System.Security.Claims;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Constants;
using AgroLink.Domain.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Caching.Memory;

namespace AgroLink.Api.Security;

public class FarmRoleHandler(
    IMemoryCache cache,
    IServiceProvider serviceProvider,
    IHttpContextAccessor httpContextAccessor
) : AuthorizationHandler<FarmRoleRequirement>
{
    private static readonly Dictionary<string, int> RoleHierarchy = new()
    {
        { FarmMemberRoles.Owner, 4 },
        { FarmMemberRoles.Admin, 3 },
        { FarmMemberRoles.Editor, 2 },
        { FarmMemberRoles.Viewer, 1 },
    };

    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        FarmRoleRequirement requirement
    )
    {
        var httpContext = httpContextAccessor.HttpContext;
        if (httpContext == null)
            return;

        // 1. Get User ID
        var userIdClaim = context.User.FindFirst("userid");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            return;
        }

        // 2. Get Farm ID (Route > Query > Header)
        int? farmId = null;

        if (httpContext.Request.RouteValues.TryGetValue("farmId", out var routeFarmId))
        {
            if (int.TryParse(routeFarmId?.ToString(), out var parsedId))
                farmId = parsedId;
        }
        else if (httpContext.Request.Query.TryGetValue("farmId", out var queryFarmId))
        {
            if (int.TryParse(queryFarmId.ToString(), out var parsedId))
                farmId = parsedId;
        }
        else if (httpContext.Request.Headers.TryGetValue("x-farm-id", out var headerFarmId))
        {
            if (int.TryParse(headerFarmId.ToString(), out var parsedId))
                farmId = parsedId;
        }

        if (!farmId.HasValue)
        {
            // If farmId is not present but required by policy, fail.
            // However, some endpoints might be mixed. For now, we fail if requirement is present.
            return;
        }

        // 3. Get Role (Cache or DB)
        var cacheKey = $"farm_auth_{userId}_{farmId}";
        if (!cache.TryGetValue(cacheKey, out string? userRole))
        {
            using var scope = serviceProvider.CreateScope();
            var farmMemberRepo = scope.ServiceProvider.GetRequiredService<IFarmMemberRepository>();
            var member = await farmMemberRepo.GetByFarmAndUserAsync(farmId.Value, userId);

            userRole = member?.Role;

            var cacheOptions = new MemoryCacheEntryOptions().SetAbsoluteExpiration(
                TimeSpan.FromMinutes(5)
            );
            cache.Set(cacheKey, userRole, cacheOptions);
        }

        if (string.IsNullOrEmpty(userRole))
        {
            return;
        }

        // 4. Validate Role Hierarchy
        if (
            RoleHierarchy.TryGetValue(userRole, out var userLevel)
            && RoleHierarchy.TryGetValue(requirement.Role, out var requiredLevel)
        )
        {
            if (userLevel >= requiredLevel)
            {
                // 5. Inject Context
                httpContext.Items["CurrentFarmId"] = farmId.Value;
                httpContext.Items["CurrentFarmRole"] = userRole;
                context.Succeed(requirement);
            }
        }
    }
}
