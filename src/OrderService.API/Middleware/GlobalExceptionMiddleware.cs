using System.Diagnostics;
using System.Net;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.BuildingBlocks.Logging;

namespace OrderService.API.Middleware;

/// <summary>
/// Catches unhandled exceptions and returns standardized error responses.
/// Logs with ILogger using CorrelationId, RequestId, TraceId, and Exception.
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

    public GlobalExceptionMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionMiddleware> logger,
        IHostEnvironment environment)
    {
        _next = next;
        _logger = logger;
        _environment = environment;
    }

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

        var correlationId = context.Items.TryGetValue(LoggingContextKeys.CorrelationId, out var c) ? c?.ToString() : null
            ?? context.Request.Headers[CorrelationLoggingMiddleware.CorrelationHeader].FirstOrDefault()
            ?? string.Empty;
        var requestId = context.Items.TryGetValue(LoggingContextKeys.RequestId, out var r) ? r?.ToString() : null
            ?? context.TraceIdentifier;
        var traceId = context.Items.TryGetValue(LoggingContextKeys.TraceId, out var t) ? t?.ToString() : null
            ?? Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        if (exception is DbUpdateException dbEx)
        {
            _logger.LogError(
                dbEx,
                "Database failure processing {RequestMethod} {RequestPath} CorrelationId={CorrelationId} RequestId={RequestId} TraceId={TraceId} Exception={ExceptionType}",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                requestId,
                traceId,
                dbEx.GetType().Name);
        }
        else
        {
            _logger.LogError(
                exception,
                "Unhandled exception processing {RequestMethod} {RequestPath} CorrelationId={CorrelationId} RequestId={RequestId} TraceId={TraceId} Exception={ExceptionType}",
                context.Request.Method,
                context.Request.Path,
                correlationId,
                requestId,
                traceId,
                exception.GetType().Name);
        }

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)statusCode;

        var problem = new
        {
            type = $"https://httpstatuses.com/{(int)statusCode}",
            title,
            status = (int)statusCode,
            detail = _environment.IsDevelopment() ? exception.Message : title,
            traceId,
            correlationId,
            requestId
        };

        await context.Response.WriteAsync(JsonSerializer.Serialize(problem, JsonOptions));
    }
}
