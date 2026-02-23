using Microsoft.AspNetCore.Authorization;

namespace AgroLink.Api.Security;

public class FarmRoleRequirement(string role) : IAuthorizationRequirement
{
    public string Role { get; } = role;
}
