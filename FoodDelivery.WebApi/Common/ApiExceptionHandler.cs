using FluentValidation;
using FoodDelivery.Application.Common.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace FoodDelivery.WebApi.Common;

/// <summary>
/// Turns application exceptions into RFC 7807 problem responses (application/problem+json).
/// Unknown exceptions become a generic 500 without internal details.
/// </summary>
public sealed class ApiExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<ApiExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException ex => new ValidationProblemDetails(
                ex.Errors
                    .GroupBy(x => x.PropertyName)
                    .ToDictionary(g => g.Key, g => g.Select(x => x.ErrorMessage).ToArray()))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred."
            },
            BusinessRuleException ex => Create(StatusCodes.Status400BadRequest, "Request cannot be completed", ex.Message),
            UnauthorizedException ex => Create(StatusCodes.Status401Unauthorized, "Unauthorized", ex.Message),
            NotFoundException ex => Create(StatusCodes.Status404NotFound, "Resource not found", ex.Message),
            ConflictException ex => Create(StatusCodes.Status409Conflict, "Conflict", ex.Message),
            _ => Create(StatusCodes.Status500InternalServerError, "Internal server error", null)
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "Unhandled exception");
        }
        else
        {
            logger.LogWarning("Request failed with {StatusCode}: {Message}", problem.Status, exception.Message);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails Create(int status, string title, string? detail)
        => new() { Status = status, Title = title, Detail = detail };
}
