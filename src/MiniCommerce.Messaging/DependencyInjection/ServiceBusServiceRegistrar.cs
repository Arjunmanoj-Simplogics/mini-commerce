using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniCommerce.AzureAuth;
using MiniCommerce.Messaging.Abstractions;
using MiniCommerce.Messaging.Options;

namespace MiniCommerce.Messaging.DependencyInjection;

/// <summary>
/// Registers Azure Service Bus publisher and optional consumer via DI.
/// Instance-based registrar (no static service locator).
/// </summary>
public sealed class ServiceBusServiceRegistrar
{
    /// <summary>
    /// Binds <see cref="ServiceBusOptions"/> and registers <see cref="IMessagePublisher"/>.
    /// When Service Bus is enabled and <paramref name="registerConsumer"/> is true,
    /// also registers <see cref="IMessageConsumer"/> and a <see cref="ServiceBusConsumerHostedService"/>.
    /// </summary>
    public IServiceCollection Register(
        IServiceCollection services,
        IConfiguration configuration,
        bool registerConsumer = false)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMiniCommerceAzureCredential(configuration);
        services.Configure<ServiceBusOptions>(configuration.GetSection(ServiceBusOptions.SectionName));

        var options = configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
            ?? new ServiceBusOptions();

        if (options.Enabled)
        {
            services.AddSingleton<IMessagePublisher, ServiceBusPublisher>();
            if (registerConsumer)
            {
                services.AddSingleton<IMessageConsumer, ServiceBusConsumer>();
                services.AddHostedService<ServiceBusConsumerHostedService>();
            }
        }
        else
        {
            services.AddSingleton<IMessagePublisher, NoOpMessagePublisher>();
        }

        return services;
    }
}
