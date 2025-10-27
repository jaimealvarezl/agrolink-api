using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace AgroLink.API.Controllers;

[ApiController]
[Authorize]
public abstract class BaseController : ControllerBase
{
    protected int GetCurrentUserId()
    {
        var userIdClaim = User.FindFirst("userid");
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            throw new UnauthorizedAccessException("Invalid user token");
        return userId;
    }

    protected ActionResult HandleServiceException(Exception ex)
    {
        return ex switch
        {
            ArgumentException => BadRequest(ex.Message),
            UnauthorizedAccessException => Unauthorized(ex.Message),
            _ => StatusCode(500, "An unexpected error occurred"),
        };
    }
}
