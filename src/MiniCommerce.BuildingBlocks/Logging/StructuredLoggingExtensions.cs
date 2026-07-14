namespace MiniCommerce.BuildingBlocks.Logging;

/// <summary>
/// Registers Mini Commerce correlation + structured request logging middleware.
/// </summary>
public static class StructuredLoggingExtensions
{
    /// <summary>
    /// Adds CorrelationId / RequestId / TraceId / SpanId LogContext enrichment and
    /// structured ILogger request timing (including Exception + ExecutionTimeMs on failures).
    /// </summary>
    public static IApplicationBuilder UseMiniCommerceStructuredLogging(this IApplicationBuilder app)
    {
        app.UseMiddleware<CorrelationLoggingMiddleware>();
        app.UseMiddleware<StructuredRequestLoggingMiddleware>();
        return app;
    }

    /// <summary>
    /// Backward-compatible alias for <see cref="UseMiniCommerceStructuredLogging"/>.
    /// </summary>
    public static IApplicationBuilder UseMiniCommerceCorrelationLogging(this IApplicationBuilder app)
        => app.UseMiniCommerceStructuredLogging();
}
