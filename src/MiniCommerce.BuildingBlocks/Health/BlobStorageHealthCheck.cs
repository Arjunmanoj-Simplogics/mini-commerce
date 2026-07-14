using Microsoft.Extensions.Diagnostics.HealthChecks;
using MiniCommerce.Storage.Abstractions;

namespace MiniCommerce.BuildingBlocks.Health;

/// <summary>
/// Readiness check for Azure Blob Storage (registered only when BlobStorage:Enabled=true).
/// </summary>
public sealed class BlobStorageHealthCheck : IHealthCheck
{
    private readonly IBlobStorageService? _blob;

    public BlobStorageHealthCheck(IServiceProvider services)
    {
        _blob = services.GetService<IBlobStorageService>();
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (_blob is null)
        {
            return HealthCheckResult.Degraded("Blob storage enabled but IBlobStorageService is not registered.");
        }

        try
        {
            _ = _blob.GetBlobUrl("health-check-probe");
            await _blob.EnsureContainerExistsAsync(cancellationToken);
            return HealthCheckResult.Healthy("Blob storage is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Blob storage unhealthy.", ex);
        }
    }
}
