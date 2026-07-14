using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using MiniCommerce.AzureAuth;
using MiniCommerce.Messaging;
using MiniCommerce.Messaging.Options;

namespace MiniCommerce.BuildingBlocks.Health;

/// <summary>
/// Readiness check for Azure Service Bus (registered only when ServiceBus:Enabled=true).
/// </summary>
public sealed class ServiceBusHealthCheck : IHealthCheck
{
    private readonly ServiceBusOptions _options;
    private readonly ServiceBusConnectivity _connectivity;

    public ServiceBusHealthCheck(IOptions<ServiceBusOptions> options, IAzureCredentialProvider credentialProvider)
    {
        _options = options.Value;
        _connectivity = new ServiceBusConnectivity(credentialProvider);
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        if (!_connectivity.CanCreate(_options))
        {
            return HealthCheckResult.Unhealthy(
                "Service Bus is enabled but ConnectionString (Development) or FullyQualifiedNamespace (Production MI) is missing.");
        }

        try
        {
            await using var client = _connectivity.CreateClient(_options);
            await using var sender = client.CreateSender(_options.TopicName);
            _ = sender.EntityPath;
            return HealthCheckResult.Healthy($"Service Bus topic '{_options.TopicName}' is reachable.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Service Bus unhealthy.", ex);
        }
    }
}
