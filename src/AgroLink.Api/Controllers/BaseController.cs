using AgroLink.Application.Common.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AgroLink.Api.Controllers;

[ApiController]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out var userId))
        {
            throw new UnauthorizedAccessException("Invalid user token");
        }

        return userId;
    }

    protected ActionResult HandleServiceException(Exception ex)
    {
        return ex switch
        {
            ArgumentException => BadRequest(ex.Message),
            UnauthorizedAccessException => Unauthorized(ex.Message),
            ForbiddenAccessException => Forbid(ex.Message),
            _ => HandleUnexpectedException(ex),
        };
    }

    private ActionResult HandleUnexpectedException(Exception ex)
    {
        var logger = HttpContext?.RequestServices?.GetService<ILogger<BaseController>>();
        logger?.LogError(
            ex,
            "An unexpected error occurred in {Controller}: {Message}",
            GetType().Name,
            ex.Message
        );

        return StatusCode(500, "An unexpected error occurred");
    }
}
