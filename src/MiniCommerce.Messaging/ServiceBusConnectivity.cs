using Azure.Messaging.ServiceBus;
using MiniCommerce.AzureAuth;
using MiniCommerce.Messaging.Internal;
using MiniCommerce.Messaging.Options;

namespace MiniCommerce.Messaging;

/// <summary>
/// Public helper for health checks and diagnostics. Uses the shared credential provider.
/// </summary>
public sealed class ServiceBusConnectivity
{
    private readonly ServiceBusClientFactory _factory;

    /// <summary>
    /// Creates connectivity helper with shared <see cref="IAzureCredentialProvider"/>.
    /// </summary>
    public ServiceBusConnectivity(IAzureCredentialProvider credentialProvider)
    {
        _factory = new ServiceBusClientFactory(credentialProvider);
    }

    /// <summary>Returns true when options can create a client.</summary>
    public bool CanCreate(ServiceBusOptions options) => _factory.CanCreate(options);

    /// <summary>Creates a disposable Service Bus client for probing.</summary>
    public ServiceBusClient CreateClient(ServiceBusOptions options) => _factory.Create(options);
}
