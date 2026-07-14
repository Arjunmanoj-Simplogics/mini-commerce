using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace MiniCommerce.BuildingBlocks.Health;

/// <summary>
/// Writes ASP.NET Core HealthReport results as standard application/json payloads.
/// </summary>
public static class HealthCheckResponseWriter
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false
    };

    /// <summary>
    /// Serializes status, duration, and per-check entries for Kubernetes probes and operators.
    /// </summary>
    public static Task WriteAsync(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json; charset=utf-8";

        var payload = new HealthCheckResponse(
            Status: report.Status.ToString(),
            TotalDuration: report.TotalDuration.ToString(),
            Checks: report.Entries
                .Select(e => new HealthCheckEntryResponse(
                    Name: e.Key,
                    Status: e.Value.Status.ToString(),
                    Description: e.Value.Description,
                    Duration: e.Value.Duration.ToString(),
                    Exception: e.Value.Exception?.Message,
                    Tags: e.Value.Tags.ToArray(),
                    Data: e.Value.Data.Count == 0
                        ? null
                        : e.Value.Data.ToDictionary(k => k.Key, v => v.Value?.ToString())))
                .ToArray());

        return context.Response.WriteAsync(JsonSerializer.Serialize(payload, SerializerOptions));
    }

    private sealed record HealthCheckResponse(
        string Status,
        string TotalDuration,
        HealthCheckEntryResponse[] Checks);

    private sealed record HealthCheckEntryResponse(
        string Name,
        string Status,
        string? Description,
        string Duration,
        string? Exception,
        string[] Tags,
        Dictionary<string, string?>? Data);
}
