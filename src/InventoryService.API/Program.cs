using System.Diagnostics;
using System.Net;
using System.Text.Json;
using FluentValidation;
using InventoryService.Application;
using InventoryService.Application.Exceptions;
using InventoryService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.AzureAuth;
using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Health;
using MiniCommerce.BuildingBlocks.Hosting;
using MiniCommerce.BuildingBlocks.Logging;
using MiniCommerce.BuildingBlocks.Observability;
using MiniCommerce.Messaging.DependencyInjection;
using Serilog;

namespace InventoryService.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .Enrich.FromLogContext()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((ctx, _, cfg) => cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", "InventoryService")
                .WriteTo.Console());

            builder.Services.AddMiniCommerceAzureCredential(builder.Configuration);
            builder.AddKeyVaultConfiguration();
            builder.AddMiniCommerceAksHosting();
            builder.Services.AddMiniCommerceOptions(builder.Configuration);
            builder.Services.AddMiniCommerceTelemetry(builder.Configuration);
            new ServiceBusServiceRegistrar().Register(builder.Services, builder.Configuration, registerConsumer: false);

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new() { Title = "Inventory Service API", Version = "v1" });
            });

            var connectionString = builder.Configuration.GetRequiredSqlConnectionString(ConnectionStringNames.InventoryDB);
            builder.Services.AddMiniCommerceHealthChecks(builder.Configuration, connectionString);
            builder.Services.AddMiniCommerceCors(builder.Configuration);

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);

            var app = builder.Build();

            app.UseMiniCommerceForwardedHeaders();
            app.UseMiniCommerceHttpsRedirection();
            app.UseMiniCommerceStructuredLogging();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.Use(async (context, next) =>
            {
                try { await next(); }
                catch (Exception ex) { await WriteError(context, ex, app.Environment); }
            });

            app.UseCors(CorsOptions.FrontendPolicyName);
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapMiniCommerceHealthEndpoints();

            if (builder.Configuration.GetSection(SqlOptions.SectionName).Get<SqlOptions>()?.AutoMigrate ?? true)
            {
                await app.Services.InitializeDatabaseAsync();
            }

            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Inventory Service terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static async Task WriteError(HttpContext context, Exception exception, IHostEnvironment env)
    {
        var (status, title) = exception switch
        {
            NotFoundException => (HttpStatusCode.NotFound, "Resource not found"),
            InsufficientStockException => (HttpStatusCode.Conflict, "Insufficient stock"),
            Application.Exceptions.ValidationException or FluentValidation.ValidationException => (HttpStatusCode.BadRequest, "Validation failed"),
            DbUpdateException => (HttpStatusCode.ServiceUnavailable, "Database operation failed"),
            _ => (HttpStatusCode.InternalServerError, "An unexpected error occurred")
        };

        var logger = context.RequestServices.GetRequiredService<ILoggerFactory>()
            .CreateLogger("InventoryService.API.Exceptions");

        var correlationId = context.Items.TryGetValue(LoggingContextKeys.CorrelationId, out var c) ? c?.ToString() : null
            ?? context.Request.Headers[CorrelationLoggingMiddleware.CorrelationHeader].FirstOrDefault()
            ?? string.Empty;
        var requestId = context.Items.TryGetValue(LoggingContextKeys.RequestId, out var r) ? r?.ToString() : null
            ?? context.TraceIdentifier;
        var traceId = context.Items.TryGetValue(LoggingContextKeys.TraceId, out var t) ? t?.ToString() : null
            ?? Activity.Current?.TraceId.ToString()
            ?? context.TraceIdentifier;

        logger.LogError(
            exception,
            "Unhandled exception for HTTP {RequestMethod} {RequestPath} CorrelationId={CorrelationId} RequestId={RequestId} TraceId={TraceId} Exception={ExceptionType}",
            context.Request.Method,
            context.Request.Path,
            correlationId,
            requestId,
            traceId,
            exception.GetType().Name);

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            title,
            status = (int)status,
            detail = env.IsDevelopment() ? exception.Message : title,
            traceId,
            correlationId,
            requestId
        }));
    }
}
