using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgroLink.Api.Filters;

public class InternalJobKeyFilter(IConfiguration configuration) : IActionFilter
{
    public void OnActionExecuting(ActionExecutingContext context)
    {
        var configuredKey = configuration["InternalJobs:JobKey"];
        var headerKey = context.HttpContext.Request.Headers["X-Internal-Job-Key"].FirstOrDefault();

        if (
            string.IsNullOrWhiteSpace(configuredKey)
            || string.IsNullOrWhiteSpace(headerKey)
            || !string.Equals(configuredKey, headerKey, StringComparison.Ordinal)
        )
        {
            context.Result = new UnauthorizedResult();
        }
    }

    public void OnActionExecuted(ActionExecutedContext context) { }
}
