using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace VehicleManager.Api.ErrorHandling;

// Turns unexpected failures into a generic problem response; details go to the log, never to the caller.
public sealed class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger, IProblemDetailsService problemDetails) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (status, title) = exception switch
        {
            BadHttpRequestException => (StatusCodes.Status400BadRequest, "The request could not be read."),
            DbUpdateException => (
                StatusCodes.Status409Conflict, "The change conflicted with another update. Reload and try again."),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred.")
        };

        switch (status)
        {
            case StatusCodes.Status500InternalServerError:
                logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                break;
            case StatusCodes.Status409Conflict:
                logger.LogWarning(exception, "Database conflict for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
                break;
            default:
                logger.LogInformation("Request rejected: {Title}", title);
                break;
        }

        httpContext.Response.StatusCode = status;

        return await problemDetails.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = new ProblemDetails { Status = status, Title = title }
        });
    }
}
