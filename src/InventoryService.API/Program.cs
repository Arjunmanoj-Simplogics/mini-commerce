using System.Net;
using System.Text.Json;
using FluentValidation;
using InventoryService.Application;
using InventoryService.Application.DTOs;
using InventoryService.Application.Exceptions;
using InventoryService.Application.Interfaces;
using InventoryService.Infrastructure;
using Microsoft.EntityFrameworkCore;
using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.Contracts.Inventory;
using Serilog;
using Serilog.Events;

namespace InventoryService.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Information()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((ctx, _, cfg) => cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .Enrich.FromLogContext()
                .WriteTo.Console());

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(o =>
            {
                o.SwaggerDoc("v1", new() { Title = "Inventory Service API", Version = "v1" });
            });

            var connectionString = builder.Configuration.GetConnectionString("InventoryDB");
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live"])
                .AddSqlServer(connectionString ?? "Server=.;Database=InventoryDB;Trusted_Connection=True;", name: "sqlserver", tags: ["ready"]);

            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
            builder.Services.AddCors(o => o.AddPolicy("FrontendPolicy", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

            builder.Services.AddApplication();
            builder.Services.AddInfrastructure(builder.Configuration);
            builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);

            var app = builder.Build();

            app.UseSerilogRequestLogging();
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

            app.UseCors("FrontendPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
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

        Log.Error(exception, "Unhandled exception");
        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = (int)status;
        await context.Response.WriteAsync(JsonSerializer.Serialize(new
        {
            title,
            status = (int)status,
            detail = env.IsDevelopment() ? exception.Message : title,
            traceId = context.TraceIdentifier
        }));
    }
}
