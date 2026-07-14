using MiniCommerce.AzureAuth;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Health;
using MiniCommerce.BuildingBlocks.Hosting;
using MiniCommerce.BuildingBlocks.Logging;
using MiniCommerce.BuildingBlocks.Observability;
using MiniCommerce.Messaging.DependencyInjection;
using NotificationService.Application;
using NotificationService.Infrastructure;
using Serilog;

namespace NotificationService.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((_, _, cfg) => cfg
                .WriteTo.Console()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", "NotificationService"));

            builder.Services.AddMiniCommerceAzureCredential(builder.Configuration);
            builder.AddKeyVaultConfiguration();
            builder.AddMiniCommerceAksHosting();
            builder.Services.AddMiniCommerceOptions(builder.Configuration);
            builder.Services.AddMiniCommerceTelemetry(builder.Configuration);
            new ServiceBusServiceRegistrar().Register(builder.Services, builder.Configuration, registerConsumer: true);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "Notification Service API", Version = "v1" }));

            var cs = builder.Configuration.GetRequiredSqlConnectionString(ConnectionStringNames.NotificationDB);
            builder.Services.AddMiniCommerceHealthChecks(builder.Configuration, cs);
            builder.Services.AddMiniCommerceCors(builder.Configuration);

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);

            var app = builder.Build();
            app.UseMiniCommerceForwardedHeaders();
            app.UseMiniCommerceHttpsRedirection();
            app.UseMiniCommerceStructuredLogging();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors(CorsOptions.FrontendPolicyName);
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
            Log.Fatal(ex, "Notification Service terminated unexpectedly");
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}
