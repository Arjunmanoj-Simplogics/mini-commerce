using System.Diagnostics;
using Serilog.Context;

namespace MiniCommerce.BuildingBlocks.Logging;

/// <summary>
/// Structured request logging via <see cref="ILogger{T}"/>.
/// Emits CorrelationId, RequestId, TraceId, SpanId, and ExecutionTimeMs on every request.
/// On unhandled exceptions (when no outer exception middleware swallows them), also logs Exception.
/// </summary>
public sealed class StructuredRequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<StructuredRequestLoggingMiddleware> _logger;

    public StructuredRequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<StructuredRequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path.Value ?? string.Empty;
        var query = context.Request.QueryString.HasValue ? context.Request.QueryString.Value : string.Empty;

        var correlationId = Resolve(context, LoggingContextKeys.CorrelationId)
            ?? context.Request.Headers[CorrelationLoggingMiddleware.CorrelationHeader].FirstOrDefault()
            ?? string.Empty;
        var requestId = Resolve(context, LoggingContextKeys.RequestId)
            ?? context.TraceIdentifier;
        var traceId = Resolve(context, LoggingContextKeys.TraceId)
            ?? Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;
        var spanId = Resolve(context, LoggingContextKeys.SpanId)
            ?? Activity.Current?.SpanId.ToString()
            ?? string.Empty;

        _logger.LogInformation(
            "Incoming HTTP {RequestMethod} {RequestPath}{QueryString} CorrelationId={CorrelationId} RequestId={RequestId} TraceId={TraceId} SpanId={SpanId}",
            method,
            path,
            query,
            correlationId,
            requestId,
            traceId,
            spanId);

        try
        {
            await _next(context);
            stopwatch.Stop();

            using (LogContext.PushProperty("ExecutionTimeMs", stopwatch.ElapsedMilliseconds))
            {
                _logger.LogInformation(
                    "Completed HTTP {RequestMethod} {RequestPath} with {StatusCode} in {ExecutionTimeMs}ms CorrelationId={CorrelationId} RequestId={RequestId} TraceId={TraceId} SpanId={SpanId}",
                    method,
                    path,
                    context.Response.StatusCode,
                    stopwatch.ElapsedMilliseconds,
                    correlationId,
                    requestId,
                    traceId,
                    spanId);
            }
        }
        catch (Exception ex)
        {
            // Reached when no downstream middleware converts exceptions to HTTP responses.
            stopwatch.Stop();

            using (LogContext.PushProperty("ExecutionTimeMs", stopwatch.ElapsedMilliseconds))
            {
                _logger.LogError(
                    ex,
                    "Unhandled exception for HTTP {RequestMethod} {RequestPath} after {ExecutionTimeMs}ms CorrelationId={CorrelationId} RequestId={RequestId} TraceId={TraceId} SpanId={SpanId} Exception={ExceptionType}",
                    method,
                    path,
                    stopwatch.ElapsedMilliseconds,
                    correlationId,
                    requestId,
                    traceId,
                    spanId,
                    ex.GetType().Name);
            }

            throw;
        }
    }

    private static string? Resolve(HttpContext context, string key)
        => context.Items.TryGetValue(key, out var value) ? value?.ToString() : null;
}
