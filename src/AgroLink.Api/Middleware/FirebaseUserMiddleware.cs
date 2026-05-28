using System.Globalization;
using System.Security.Claims;
using AgroLink.Application.Interfaces;
using AgroLink.Domain.Entities;

namespace AgroLink.Api.Middleware;

public class FirebaseUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, IAuthRepository authRepository)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var firebaseUid =
                context.User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? context.User.FindFirst("sub")?.Value;

            if (!string.IsNullOrEmpty(firebaseUid))
            {
                var user = await authRepository.GetUserByFirebaseUidAsync(firebaseUid);

                if (user == null)
                {
                    user = await ProvisionUserAsync(context, authRepository, firebaseUid);
                }

                if (user?.IsActive == true)
                {
                    context.User.AddIdentity(
                        new ClaimsIdentity([
                            new Claim("userid", user.Id.ToString(CultureInfo.InvariantCulture)),
                            new Claim("role", user.Role),
                        ])
                    );
                }
            }
        }

        await next(context);
    }

    private static async Task<User?> ProvisionUserAsync(
        HttpContext context,
        IAuthRepository authRepository,
        string firebaseUid
    )
    {
        var email =
            context.User.FindFirst(ClaimTypes.Email)?.Value
            ?? context.User.FindFirst("email")?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return null;
        }

        var existingByEmail = await authRepository.GetUserByEmailAsync(email);
        if (existingByEmail != null)
        {
            existingByEmail.FirebaseUid = firebaseUid;
            await authRepository.UpdateUserAsync(existingByEmail);
            return existingByEmail;
        }

        var name =
            context.User.FindFirst(ClaimTypes.Name)?.Value
            ?? context.User.FindFirst("name")?.Value
            ?? email;
        var newUser = new User
        {
            FirebaseUid = firebaseUid,
            Email = email,
            Name = name,
            Role = "USER",
            IsActive = true,
        };

        await authRepository.AddUserAsync(newUser);
        return newUser;
    }
}
