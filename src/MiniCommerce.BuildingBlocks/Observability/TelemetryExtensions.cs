using System.Diagnostics;
using Azure.Monitor.OpenTelemetry.AspNetCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Logging;
using OpenTelemetry.Exporter;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace MiniCommerce.BuildingBlocks.Observability;

/// <summary>
/// Production OpenTelemetry registration: distributed tracing, metrics, and structured log export.
/// Instruments ASP.NET Core, HttpClient, EF Core, SqlClient, Azure Service Bus, and Azure Storage.
/// Exporters: Console, OTLP, Azure Monitor (optional).
/// </summary>
public static class TelemetryExtensions
{
    /// <summary>
    /// Registers OpenTelemetry tracing, metrics, and logging with configurable exporters.
    /// Does not change business logic — observability wiring only.
    /// </summary>
    public static IServiceCollection AddMiniCommerceTelemetry(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        // Azure SDK (Blob, Service Bus, …) emit ActivitySource spans when this switch is on.
        AppContext.SetSwitch("Azure.Experimental.EnableActivitySource", true);

        services.Configure<OpenTelemetryOptions>(configuration.GetSection(OpenTelemetryOptions.SectionName));
        services.Configure<ApplicationInsightsOptions>(configuration.GetSection(ApplicationInsightsOptions.SectionName));

        var otel = configuration.GetSection(OpenTelemetryOptions.SectionName).Get<OpenTelemetryOptions>()
            ?? new OpenTelemetryOptions();
        var ai = configuration.GetSection(ApplicationInsightsOptions.SectionName).Get<ApplicationInsightsOptions>()
            ?? new ApplicationInsightsOptions();

        if (string.IsNullOrWhiteSpace(ai.ConnectionString))
        {
            ai.ConnectionString = configuration["Observability:ApplicationInsightsConnectionString"];
        }

        var azureMonitorEnabled = otel.Exporters.AzureMonitor
            && !string.IsNullOrWhiteSpace(ai.ConnectionString);

        if (!otel.Enabled && !azureMonitorEnabled)
        {
            return services;
        }

        var serviceName = ResolveServiceName(configuration, otel);
        var serviceVersion = otel.ServiceVersion
            ?? typeof(TelemetryExtensions).Assembly.GetName().Version?.ToString()
            ?? "1.0.0";

        var resourceBuilder = ResourceBuilder.CreateDefault()
            .AddService(
                serviceName: serviceName,
                serviceVersion: serviceVersion,
                serviceInstanceId: Environment.MachineName)
            .AddAttributes([
                new KeyValuePair<string, object>(
                    "deployment.environment",
                    configuration["ASPNETCORE_ENVIRONMENT"]
                        ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                        ?? "Production")
            ]);

        var builder = services.AddOpenTelemetry()
            .ConfigureResource(r => r
                .AddService(
                    serviceName: serviceName,
                    serviceVersion: serviceVersion,
                    serviceInstanceId: Environment.MachineName)
                .AddAttributes([
                    new KeyValuePair<string, object>(
                        "deployment.environment",
                        configuration["ASPNETCORE_ENVIRONMENT"]
                            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
                            ?? "Production")
                ]));

        if (otel.Enabled)
        {
            services.Configure<OpenTelemetryLoggerOptions>(options =>
            {
                options.IncludeFormattedMessage = true;
                options.IncludeScopes = true;
                options.ParseStateValues = true;
            });

            builder.WithTracing(tracing =>
            {
                tracing
                    .SetResourceBuilder(resourceBuilder)
                    .AddSource(MiniCommerceTelemetry.ActivitySourceName)
                    .AddSource(MiniCommerceTelemetry.ServiceBusSourceName)
                    .AddSource(MiniCommerceTelemetry.StorageSourceName)
                    .AddSource(MiniCommerceTelemetry.AzureSourceName)
                    .AddAspNetCoreInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.Filter = httpContext =>
                        {
                            if (!otel.ExcludeHealthChecks)
                            {
                                return true;
                            }

                            var path = httpContext.Request.Path.Value ?? string.Empty;
                            return !IsHealthPath(path);
                        };
                        options.EnrichWithHttpRequest = (activity, request) =>
                        {
                            var correlationId = request.Headers[CorrelationLoggingMiddleware.CorrelationHeader]
                                .FirstOrDefault();
                            if (!string.IsNullOrWhiteSpace(correlationId))
                            {
                                activity.SetTag("correlation.id", correlationId);
                            }
                        };
                    })
                    .AddHttpClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                    })
                    .AddSqlClientInstrumentation(options =>
                    {
                        options.RecordException = true;
                        options.SetDbStatementForText = otel.CaptureSqlText;
                    })
                    .AddEntityFrameworkCoreInstrumentation(options =>
                    {
                        options.SetDbStatementForText = otel.CaptureSqlText;
                    });

                if (otel.Exporters.Console)
                {
                    tracing.AddConsoleExporter();
                }

                if (otel.Exporters.Otlp)
                {
                    tracing.AddOtlpExporter(o => ConfigureOtlp(o, otel));
                }
            });

            builder.WithMetrics(metrics =>
            {
                metrics
                    .SetResourceBuilder(resourceBuilder)
                    .AddMeter(MiniCommerceTelemetry.MeterName)
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation();

                if (otel.Exporters.Console)
                {
                    metrics.AddConsoleExporter();
                }

                if (otel.Exporters.Otlp)
                {
                    metrics.AddOtlpExporter(o => ConfigureOtlp(o, otel));
                }
            });

            builder.WithLogging(logging =>
            {
                logging.SetResourceBuilder(resourceBuilder);

                if (otel.Exporters.Console)
                {
                    logging.AddConsoleExporter();
                }

                if (otel.Exporters.Otlp)
                {
                    logging.AddOtlpExporter(o => ConfigureOtlp(o, otel));
                }
            });
        }

        if (azureMonitorEnabled)
        {
            builder.UseAzureMonitor(options =>
            {
                options.ConnectionString = ai.ConnectionString;
            });
        }

        return services;
    }

    private static void ConfigureOtlp(OtlpExporterOptions options, OpenTelemetryOptions otel)
    {
        if (!string.IsNullOrWhiteSpace(otel.OtlpEndpoint)
            && Uri.TryCreate(otel.OtlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            options.Endpoint = endpoint;
        }

        options.Protocol = string.Equals(otel.OtlpProtocol, "HttpProtobuf", StringComparison.OrdinalIgnoreCase)
            ? OtlpExportProtocol.HttpProtobuf
            : OtlpExportProtocol.Grpc;
    }

    private static string ResolveServiceName(IConfiguration configuration, OpenTelemetryOptions otel)
    {
        if (!string.IsNullOrWhiteSpace(otel.ServiceName))
        {
            return otel.ServiceName;
        }

        return configuration["ApplicationName"]
            ?? configuration["ServiceName"]
            ?? AppDomain.CurrentDomain.FriendlyName;
    }

    private static bool IsHealthPath(string path)
        => path.StartsWith("/health", StringComparison.OrdinalIgnoreCase)
           || path.StartsWith("/api/health", StringComparison.OrdinalIgnoreCase);
}
