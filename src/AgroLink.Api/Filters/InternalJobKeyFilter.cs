using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgroLink.Api.Filters;

public class InternalJobKeyFilter(IConfiguration configuration) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var configuredKey = configuration["InternalJobs:JobKey"];
        var headerKey = context.HttpContext.Request.Headers["X-Internal-Job-Key"].FirstOrDefault();

        if (string.IsNullOrWhiteSpace(configuredKey) || string.IsNullOrWhiteSpace(headerKey))
        {
            context.Result = new UnauthorizedResult();
            return;
        }

        var configuredKeyBytes = Encoding.UTF8.GetBytes(configuredKey);
        var headerKeyBytes = Encoding.UTF8.GetBytes(headerKey);
        if (!CryptographicOperations.FixedTimeEquals(configuredKeyBytes, headerKeyBytes))
        {
            context.Result = new UnauthorizedResult();
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
