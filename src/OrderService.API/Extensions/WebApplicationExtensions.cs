using MiniCommerce.BuildingBlocks.Auth;
using OrderService.API.Configuration;
using OrderService.API.Middleware;
using OrderService.Application;
using OrderService.Infrastructure;
using Serilog;

namespace OrderService.API.Extensions;

/// <summary>
/// Extension methods for configuring the Order Service API.
/// </summary>
public static class WebApplicationExtensions
{
    /// <summary>
    /// Configures middleware pipeline for the Order Service API.
    /// </summary>
    /// <param name="app">The web application.</param>
    /// <returns>The web application.</returns>
    public static WebApplication ConfigurePipeline(this WebApplication app)
    {
        app.UseSerilogRequestLogging();

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
        app.UseMiddleware<RequestLoggingMiddleware>();

        app.UseHttpsRedirection();
        app.UseCors("FrontendPolicy");
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.MapHealthChecks("/api/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = _ => true
        });

        app.MapHealthChecks("/api/health/live", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("live")
        });

        app.MapHealthChecks("/api/health/ready", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
        {
            Predicate = check => check.Tags.Contains("ready")
        });

        return app;
    }

    /// <summary>
    /// Registers API services and dependencies.
    /// </summary>
    /// <param name="builder">The web application builder.</param>
    /// <returns>The web application builder.</returns>
    public static WebApplicationBuilder AddOrderServiceApi(this WebApplicationBuilder builder)
    {
        builder.Services.Configure<ObservabilityOptions>(
            builder.Configuration.GetSection(ObservabilityOptions.SectionName));

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

        var connectionString = builder.Configuration.GetConnectionString("OrderDB");
        builder.Services.AddHealthChecks()
            .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
            .AddSqlServer(
                connectionString ?? "Server=.;Database=OrderDB;Trusted_Connection=True;",
                name: "sqlserver",
                tags: ["ready"]);

        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        builder.Services.AddCors(options =>
        {
            options.AddPolicy("FrontendPolicy", policy =>
            {
                policy.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        builder.Services.AddApplication();
        builder.Services.AddInfrastructure(builder.Configuration);
        builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);

        var inventoryBaseUrl = builder.Configuration["Services:Inventory"] ?? "http://localhost:8081";
        var notificationBaseUrl = builder.Configuration["Services:Notification"] ?? "http://localhost:8082";

        builder.Services.AddHttpClient<OrderService.Application.Interfaces.IInventoryClient, OrderService.Infrastructure.Integration.InventoryHttpClient>(client =>
        {
            client.BaseAddress = new Uri(inventoryBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        builder.Services.Configure<OrderService.Infrastructure.Integration.ServiceBusOptions>(
            builder.Configuration.GetSection(OrderService.Infrastructure.Integration.ServiceBusOptions.SectionName));

        var serviceBusEnabled = builder.Configuration.GetValue("ServiceBus:Enabled", false);
        if (serviceBusEnabled)
        {
            builder.Services.AddSingleton<OrderService.Application.Interfaces.IIntegrationEventPublisher,
                OrderService.Infrastructure.Integration.ServiceBusIntegrationEventPublisher>();
        }
        else
        {
            builder.Services.AddHttpClient<OrderService.Application.Interfaces.IIntegrationEventPublisher,
                OrderService.Infrastructure.Integration.NotificationHttpPublisher>(client =>
            {
                client.BaseAddress = new Uri(notificationBaseUrl);
                client.Timeout = TimeSpan.FromSeconds(30);
            });
        }

        return builder;
    }
}
