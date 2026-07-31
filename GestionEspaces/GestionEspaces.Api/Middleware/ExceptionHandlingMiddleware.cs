using GestionEspaces.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;

namespace GestionEspaces.Api.Middleware;

/// <summary>
/// Converts unhandled exceptions into RFC7807 problem details responses.
/// </summary>
public sealed class ExceptionHandlingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ExceptionHandlingMiddleware> _logger;

    public ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (ConcurrencyConflictException exception)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status409Conflict, "Concurrency conflict", exception.Message);
        }
        catch (BusinessRuleViolationException exception)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status409Conflict, "Business rule violated", exception.Message);
        }
        catch (DomainException exception)
        {
            await WriteProblemDetailsAsync(context, StatusCodes.Status400BadRequest, "Domain error", exception.Message);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Unhandled exception while processing request.");
            await WriteProblemDetailsAsync(context, StatusCodes.Status500InternalServerError, "An unexpected error occurred", "The request could not be completed.");
        }
    }

    private static async Task WriteProblemDetailsAsync(HttpContext context, int statusCode, string title, string detail)
    {
        if (context.Response.HasStarted)
        {
            throw new InvalidOperationException("The response has already started.");
        }

        context.Response.Clear();
        context.Response.StatusCode = statusCode;
        context.Response.ContentType = "application/problem+json";

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = title,
            Detail = detail,
            Instance = context.Request.Path
        };

        await context.Response.WriteAsJsonAsync(problemDetails);
    }
}