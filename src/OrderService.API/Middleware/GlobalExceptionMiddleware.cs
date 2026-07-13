using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace OrderService.API.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns standardized error responses.
/// </summary>
public class GlobalExceptionMiddleware
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionMiddleware> _logger;
    private readonly IHostEnvironment _environment;

    /// <summary>
    /// Initializes a new instance of the <see cref="GlobalExceptionMiddleware"/> class.
    /// </summary>
    /// <param name="next">The next middleware delegate.</param>
    /// <param name="logger">The logger.</param>
    /// <param name="environment">The hosting environment.</param>
    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

    /// <summary>
    /// Invokes the middleware.
    /// </summary>
    /// <param name="context">The HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(context, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        var (statusCode, title) = exception switch
        {
            Application.Exceptions.NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            Application.Exceptions.ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            FluentValidation.ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            DbUpdateException => (HttpStatusCode.ServiceUnavailable, "Database operation failed"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        if (exception is DbUpdateException dbEx)
        {
            _logger.LogError(dbEx, "Database failure processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }
        else
        {
            _logger.LogError(exception, "Unhandled exception processing {Method} {Path}", context.Request.Method, context.Request.Path);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = _environment.IsDevelopment() ? exception.Message : title,
            traceId = context.TraceIdentifier
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
