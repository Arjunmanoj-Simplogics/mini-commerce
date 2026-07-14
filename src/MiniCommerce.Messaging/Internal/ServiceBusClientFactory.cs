using Azure.Messaging.ServiceBus;
using MiniCommerce.AzureAuth;
using MiniCommerce.Messaging.Options;

namespace MiniCommerce.Messaging.Internal;

/// <summary>
/// Creates <see cref="ServiceBusClient"/> instances with exponential backoff retry.
/// Development: connection string. Production: FullyQualifiedNamespace + shared DefaultAzureCredential.
/// </summary>
internal sealed class ServiceBusClientFactory
{
    private readonly IAzureCredentialProvider _credentialProvider;

    public ServiceBusClientFactory(IAzureCredentialProvider credentialProvider)
    {
        _credentialProvider = credentialProvider;
    }

    /// <summary>
    /// Returns true when enough settings exist to create a client.
    /// </summary>
    public bool CanCreate(ServiceBusOptions options)
    {
        if (_credentialProvider.PreferManagedIdentity)
        {
            return !string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace);
        }

        return !string.IsNullOrWhiteSpace(options.ConnectionString) ||
               !string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace);
    }

    /// <summary>
    /// Builds a client with SDK-level exponential retry.
    /// </summary>
    public ServiceBusClient Create(ServiceBusOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var clientOptions = new ServiceBusClientOptions
        {
            RetryOptions = new ServiceBusRetryOptions
            {
                Mode = ServiceBusRetryMode.Exponential,
                MaxRetries = Math.Max(0, options.MaxRetryCount),
                Delay = TimeSpan.FromSeconds(Math.Max(0, options.RetryDelaySeconds)),
                MaxDelay = TimeSpan.FromSeconds(Math.Max(1, options.MaxRetryDelaySeconds))
            }
        };

        var preferMi = _credentialProvider.PreferManagedIdentity;

        if (!preferMi && !string.IsNullOrWhiteSpace(options.ConnectionString))
        {
            return new ServiceBusClient(options.ConnectionString, clientOptions);
        }

        if (!string.IsNullOrWhiteSpace(options.FullyQualifiedNamespace))
        {
            return new ServiceBusClient(
                options.FullyQualifiedNamespace,
                _credentialProvider.Credential,
                clientOptions);
        }

        throw new InvalidOperationException(
            "Service Bus requires ConnectionString (Development) or FullyQualifiedNamespace (Production Managed Identity).");
    }
}
