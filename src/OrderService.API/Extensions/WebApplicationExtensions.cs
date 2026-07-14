using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Health;
using MiniCommerce.BuildingBlocks.Hosting;
using MiniCommerce.BuildingBlocks.Logging;
using MiniCommerce.BuildingBlocks.Observability;
using MiniCommerce.Messaging.DependencyInjection;
using MiniCommerce.Messaging.Options;
using OrderService.API.Middleware;
using OrderService.Application;
using OrderService.Infrastructure;

namespace OrderService.API.Extensions;

/// <summary>
/// Extension methods for configuring the Order Service API.
/// </summary>
public static class WebApplicationExtensions
{
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseMiniCommerceForwardedHeaders();
        app.UseMiniCommerceStructuredLogging();

        if (app.Environment.IsDevelopment())
        {
            app.UseSwagger();
            app.UseSwaggerUI(options =>
            {
                options.SwaggerEndpoint("/swagger/v1/swagger.json", "Order Service API v1");
                options.RoutePrefix = "swagger";
            });
        }

        app.UseMiddleware<GlobalExceptionMiddleware>();

        app.UseMiniCommerceHttpsRedirection();
        app.UseCors(CorsOptions.FrontendPolicyName);
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();
        app.MapMiniCommerceHealthEndpoints();

        return app;
    }

    public static WebApplicationBuilder AddOrderServiceApi(this WebApplicationBuilder builder)
    {
        builder.AddMiniCommerceAksHosting();
        builder.Services.AddMiniCommerceOptions(builder.Configuration);
        builder.Services.AddMiniCommerceTelemetry(builder.Configuration);
        new ServiceBusServiceRegistrar().Register(builder.Services, builder.Configuration, registerConsumer: false);

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new Microsoft.OpenApi.Models.OpenApiInfo
            {
                Title = "Order Service API",
                Version = "v1",
                Description = "Mini Commerce Order microservice"
            });

            var xmlFile = $"{typeof(WebApplicationExtensions).Assembly.GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            if (File.Exists(xmlPath))
            {
                options.IncludeXmlComments(xmlPath);
            }
        });

        var connectionString = builder.Configuration.GetRequiredSqlConnectionString(ConnectionStringNames.OrderDB);
        builder.Services.AddMiniCommerceHealthChecks(builder.Configuration, connectionString);
        builder.Services.AddMiniCommerceCors(builder.Configuration);

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);

        var downstream = builder.Configuration.GetSection(DownstreamServicesOptions.SectionName).Get<DownstreamServicesOptions>()
            ?? new DownstreamServicesOptions();
        var serviceBus = builder.Configuration.GetSection(ServiceBusOptions.SectionName).Get<ServiceBusOptions>()
            ?? new ServiceBusOptions();
        var httpTimeout = TimeSpan.FromSeconds(Math.Max(1, downstream.HttpClientTimeoutSeconds));

        RequireAbsoluteServiceUrl(downstream.Inventory, "Services:Inventory", "Services__Inventory");

        builder.Services.AddHttpClient<OrderService.Application.Interfaces.IInventoryClient, OrderService.Infrastructure.Integration.InventoryHttpClient>(client =>
        {
            client.BaseAddress = new Uri(downstream.Inventory);
            client.Timeout = httpTimeout;
        });

        if (serviceBus.Enabled)
        {
            builder.Services.AddSingleton<OrderService.Application.Interfaces.IIntegrationEventPublisher,
                OrderService.Infrastructure.Integration.ServiceBusIntegrationEventPublisher>();
        }
        else
        {
            RequireAbsoluteServiceUrl(downstream.Notification, "Services:Notification", "Services__Notification");
            builder.Services.AddHttpClient<OrderService.Application.Interfaces.IIntegrationEventPublisher,
                OrderService.Infrastructure.Integration.NotificationHttpPublisher>(client =>
            {
                client.BaseAddress = new Uri(downstream.Notification);
                client.Timeout = httpTimeout;
            });
        }

        return builder;
    }

    private static void RequireAbsoluteServiceUrl(string? url, string configKey, string envKey)
    {
        if (string.IsNullOrWhiteSpace(url)
            || !Uri.TryCreate(url, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{configKey} must be an absolute http(s) URL. Set {envKey} for Kubernetes " +
                $"(e.g. http://inventory-service:8080). Localhost defaults are not used.");
        }
    }
}
