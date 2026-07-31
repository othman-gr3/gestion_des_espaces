using GestionEspaces.Application.Common;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Common;

/// <summary>
/// Translates application results into HTTP responses.
/// </summary>
public static class ControllerResultExtensions
{
    public static IActionResult ToActionResult<T>(this ControllerBase controller, Result<T> result, Func<T, IActionResult> successFactory)
    {
        if (result.IsSuccess)
        {
            return successFactory(result.Value!);
        }

        if (result.Errors.Any(error => string.Equals(error.Code, "ValidationError", StringComparison.OrdinalIgnoreCase)))
        {
            var validationErrors = result.Errors
                .Where(error => string.Equals(error.Code, "ValidationError", StringComparison.OrdinalIgnoreCase))
                .GroupBy(error => error.PropertyName ?? string.Empty)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());

            return controller.ValidationProblem(new ValidationProblemDetails(validationErrors)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Instance = controller.HttpContext.Request.Path
            });
        }

        var statusCode = result.Errors.Any(error => error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            ? StatusCodes.Status404NotFound
            : result.Errors.Any(error =>
                error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error.Code, "BusinessRuleViolation", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error.Code, "ConcurrencyConflict", StringComparison.OrdinalIgnoreCase))
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

        var problemDetails = new ProblemDetails
        {
            Title = statusCode == StatusCodes.Status404NotFound ? "Resource not found" : statusCode == StatusCodes.Status409Conflict ? "Conflict" : "Request rejected",
            Detail = string.Join(" ", result.Errors.Select(error => error.Message)),
            Status = statusCode,
            Instance = controller.HttpContext.Request.Path
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    public static IActionResult ToActionResult(this ControllerBase controller, Result result)
    {
        if (result.IsSuccess)
        {
            return controller.NoContent();
        }

        if (result.Errors.Any(error => string.Equals(error.Code, "ValidationError", StringComparison.OrdinalIgnoreCase)))
        {
            var validationErrors = result.Errors
                .Where(error => string.Equals(error.Code, "ValidationError", StringComparison.OrdinalIgnoreCase))
                .GroupBy(error => error.PropertyName ?? string.Empty)
                .ToDictionary(group => group.Key, group => group.Select(error => error.Message).ToArray());

            return controller.ValidationProblem(new ValidationProblemDetails(validationErrors)
            {
                Title = "Validation failed",
                Status = StatusCodes.Status400BadRequest,
                Instance = controller.HttpContext.Request.Path
            });
        }

        var statusCode = result.Errors.Any(error => error.Code.Contains("NotFound", StringComparison.OrdinalIgnoreCase))
            ? StatusCodes.Status404NotFound
            : result.Errors.Any(error =>
                error.Code.Contains("Duplicate", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error.Code, "BusinessRuleViolation", StringComparison.OrdinalIgnoreCase)
                || string.Equals(error.Code, "ConcurrencyConflict", StringComparison.OrdinalIgnoreCase))
                ? StatusCodes.Status409Conflict
                : StatusCodes.Status400BadRequest;

        var problemDetails = new ProblemDetails
        {
            Title = statusCode == StatusCodes.Status404NotFound ? "Resource not found" : statusCode == StatusCodes.Status409Conflict ? "Conflict" : "Request rejected",
            Detail = string.Join(" ", result.Errors.Select(error => error.Message)),
            Status = statusCode,
            Instance = controller.HttpContext.Request.Path
        };

        return new ObjectResult(problemDetails)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }
}