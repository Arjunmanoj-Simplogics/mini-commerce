using MiniCommerce.Contracts.Events;
using NotificationService.Application;
using NotificationService.Application.Interfaces;
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
            builder.Host.UseSerilog((_, _, cfg) => cfg.WriteTo.Console().Enrich.FromLogContext());

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o => o.SwaggerDoc("v1", new() { Title = "Notification Service API", Version = "v1" }));

            var cs = builder.Configuration.GetConnectionString("NotificationDB");
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
                .AddSqlServer(cs ?? "Server=.;Database=NotificationDB;Trusted_Connection=True;", name: "sqlserver", tags: ["ready"]);

            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
            builder.Services.AddCors(o => o.AddPolicy("FrontendPolicy", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);

            var app = builder.Build();
            app.UseSerilogRequestLogging();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("FrontendPolicy");
            app.MapControllers();
            app.MapHealthChecks("/api/health");
            app.MapHealthChecks("/api/health/live", new() { Predicate = c => c.Tags.Contains("live") });
            app.MapHealthChecks("/api/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });

            if (builder.Configuration.GetValue("Database:AutoMigrate", true))
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
