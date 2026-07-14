using System.Diagnostics;
using System.Security.Claims;
using Serilog.Context;

namespace MiniCommerce.BuildingBlocks.Logging;

/// <summary>
/// Pushes CorrelationId, RequestId, TraceId, SpanId, UserId, and ServiceName into Serilog LogContext
/// for every request. Consumes or creates <c>X-Correlation-ID</c>.
/// </summary>
public sealed class CorrelationLoggingMiddleware
{
    public const string CorrelationHeader = "X-Correlation-ID";
    public const string TraceIdHeader = "X-Trace-Id";
    public const string SpanIdHeader = "X-Span-Id";

    private readonly RequestDelegate _next;
    private readonly string _serviceName;

    public CorrelationLoggingMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next;
        _serviceName = configuration["ApplicationName"]
            ?? configuration["ServiceName"]
            ?? AppDomain.CurrentDomain.FriendlyName;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var activity = Activity.Current;
        var traceId = activity?.TraceId.ToString()
            ?? context.TraceIdentifier
            ?? Guid.NewGuid().ToString("N");
        var spanId = activity?.SpanId.ToString() ?? string.Empty;

        var correlationId = context.Request.Headers[CorrelationHeader].FirstOrDefault()
            ?? activity?.GetBaggageItem("correlation.id")
            ?? activity?.Id
            ?? traceId
            ?? Guid.NewGuid().ToString("N");

        // Prefer keeping ASP.NET TraceIdentifier as the request id; fall back to correlation.
        if (string.IsNullOrWhiteSpace(context.TraceIdentifier))
        {
            context.TraceIdentifier = Guid.NewGuid().ToString("N");
        }

        var requestId = context.TraceIdentifier;

        context.Response.Headers[CorrelationHeader] = correlationId;
        context.Response.Headers[TraceIdHeader] = traceId;
        if (!string.IsNullOrEmpty(spanId))
        {
            context.Response.Headers[SpanIdHeader] = spanId;
        }

        activity?.SetTag("correlation.id", correlationId);
        activity?.SetBaggage("correlation.id", correlationId);

        var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? context.User?.FindFirstValue("sub")
            ?? context.User?.Identity?.Name;

        using (LogContext.PushProperty("CorrelationId", correlationId))
        using (LogContext.PushProperty("RequestId", requestId))
        using (LogContext.PushProperty("TraceId", traceId))
        using (LogContext.PushProperty("SpanId", spanId))
        using (LogContext.PushProperty("UserId", userId ?? string.Empty))
        using (LogContext.PushProperty("ServiceName", _serviceName))
        using (LogContext.PushProperty("Timestamp", DateTime.UtcNow))
        {
            context.Items[LoggingContextKeys.CorrelationId] = correlationId;
            context.Items[LoggingContextKeys.RequestId] = requestId;
            context.Items[LoggingContextKeys.TraceId] = traceId;
            context.Items[LoggingContextKeys.SpanId] = spanId;

            await _next(context);
        }
    }
}

/// <summary>
/// HttpContext.Items keys for structured logging fields.
/// </summary>
public static class LoggingContextKeys
{
    public const string CorrelationId = "MiniCommerce.CorrelationId";
    public const string RequestId = "MiniCommerce.RequestId";
    public const string TraceId = "MiniCommerce.TraceId";
    public const string SpanId = "MiniCommerce.SpanId";
}
