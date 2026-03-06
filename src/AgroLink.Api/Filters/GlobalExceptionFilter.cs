using System.Net;
using AgroLink.Application.Common.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AgroLink.Api.Filters;

public class GlobalExceptionFilter(ILogger<GlobalExceptionFilter> logger) : IExceptionFilter
{
    public void OnException(ExceptionContext context)
    {
        var ex = context.Exception;

        var (statusCode, message) = ex switch
        {
            ArgumentException => (HttpStatusCode.BadRequest, ex.Message),
            UnauthorizedAccessException => (HttpStatusCode.Unauthorized, ex.Message),
            ForbiddenAccessException => (HttpStatusCode.Forbidden, ex.Message),
            NotFoundException => (HttpStatusCode.NotFound, ex.Message),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred"),
        };

        if (statusCode == HttpStatusCode.InternalServerError)
        {
            logger.LogError(
                ex,
                "An unexpected error occurred in {Controller}: {Message}",
                context.ActionDescriptor.DisplayName,
                ex.Message
            );

            context.Result = new ObjectResult(
                new
                {
                    message = "An unexpected error occurred",
                    detail = ex.Message,
                    exception = ex.ToString(),
                }
            )
            {
                StatusCode = (int)statusCode,
            };
        }
        else
        {
            context.Result = new ObjectResult(message) { StatusCode = (int)statusCode };
        }

        context.ExceptionHandled = true;
    }
}
