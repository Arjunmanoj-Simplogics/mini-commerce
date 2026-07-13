using OrderService.API.Configuration;
using OrderService.API.Extensions;
using OrderService.Infrastructure;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .Enrich.WithMachineName()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting Order Service API");

    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext()
        .Enrich.WithEnvironmentName()
        .Enrich.WithMachineName()
        .WriteTo.Console());

    builder.AddKeyVaultConfiguration();
    builder.AddOrderServiceApi();

    var app = builder.Build();
    app.ConfigurePipeline();

    if (builder.Configuration.GetValue("Database:AutoMigrate", true))
    {
        await app.Services.InitializeDatabaseAsync();
    }

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Order Service API terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}
