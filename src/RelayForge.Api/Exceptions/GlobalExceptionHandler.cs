
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace RelayForge.Api.Exceptions;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var (statusCode, title) = exception switch
        {
            KeyNotFoundException =>
                (StatusCodes.Status404NotFound, "Resource not found"),

            ArgumentException =>
                (StatusCodes.Status400BadRequest, "Invalid request"),

            _ =>
                (StatusCodes.Status500InternalServerError, "Internal server error")
        };

        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError("Unhandled exception ({ErrorType}), trace {TraceId}",
                exception.GetType().Name, httpContext.TraceIdentifier);
        }

        httpContext.Response.StatusCode = statusCode;

        return await problemDetailsService.TryWriteAsync(
            new ProblemDetailsContext
            {
                HttpContext = httpContext,
                Exception = exception,
                ProblemDetails = new ProblemDetails
                {
                    Status = statusCode,
                    Title = title,
                    Detail = statusCode == 500
                        ? "An unexpected error occurred."
                        : exception.Message
                }
            });
    }
}
