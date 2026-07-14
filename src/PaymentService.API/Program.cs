using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCommerce.AzureAuth;
using MiniCommerce.BuildingBlocks.Auth;
using MiniCommerce.BuildingBlocks.Configuration;
using MiniCommerce.BuildingBlocks.Health;
using MiniCommerce.BuildingBlocks.Hosting;
using MiniCommerce.BuildingBlocks.Logging;
using MiniCommerce.BuildingBlocks.Observability;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Messaging;
using MiniCommerce.Messaging.Abstractions;
using MiniCommerce.Messaging.DependencyInjection;
using Serilog;

namespace PaymentService.API;

public record ChargeRequest(
    decimal Amount,
    string Currency,
    string CardHolder,
    string CardNumber,
    int ExpiryMonth,
    int ExpiryYear,
    string Cvv);

public record PaymentDto(
    Guid PaymentId,
    string Status,
    string Message,
    decimal ChargedAmount,
    string Currency,
    string Last4,
    DateTime CreatedAtUtc);

[ApiController]
[Route("api/payments")]
public class PaymentsController : ControllerBase
{
    private readonly IPaymentStore _store;
    private readonly IMessagePublisher _messagePublisher;
    private readonly ILogger<PaymentsController> _logger;

    public PaymentsController(
        IPaymentStore store,
        IMessagePublisher messagePublisher,
        ILogger<PaymentsController> logger)
    {
        _store = store;
        _messagePublisher = messagePublisher;
        _logger = logger;
    }

    /// <summary>
    /// Mock charge. Cards ending in 0000 fail; otherwise succeeds.
    /// On success, publishes PaymentCompleted (when Service Bus is enabled).
    /// </summary>
    [HttpPost("charge")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> Charge([FromBody] ChargeRequest request, CancellationToken cancellationToken)
    {
        if (request.Amount <= 0)
        {
            return BadRequest(new { title = "Amount must be greater than zero." });
        }

        var digits = new string((request.CardNumber ?? string.Empty).Where(char.IsDigit).ToArray());
        if (digits.Length < 12 || digits.Length > 19)
        {
            return BadRequest(new { title = "Enter a valid card number." });
        }

        if (request.ExpiryMonth is < 1 or > 12 || request.ExpiryYear < DateTime.UtcNow.Year)
        {
            return BadRequest(new { title = "Card expiry is invalid." });
        }

        if (string.IsNullOrWhiteSpace(request.Cvv) || request.Cvv.Length is < 3 or > 4)
        {
            return BadRequest(new { title = "CVV is invalid." });
        }

        var last4 = digits[^4..];
        var failed = last4 == "0000";
        var payment = new PaymentDto(
            Guid.NewGuid(),
            failed ? "Failed" : "Succeeded",
            failed
                ? "Mock gateway declined the card (use any card not ending in 0000)."
                : "Mock payment authorized.",
            request.Amount,
            string.IsNullOrWhiteSpace(request.Currency) ? "USD" : request.Currency.Trim().ToUpperInvariant(),
            last4,
            DateTime.UtcNow);

        await _store.SaveAsync(payment, cancellationToken);

        if (!failed)
        {
            try
            {
                await _messagePublisher.PublishAsync(
                    ServiceBusNames.PaymentCompleted,
                    new PaymentCompletedEvent
                    {
                        PaymentId = payment.PaymentId,
                        ChargedAmount = payment.ChargedAmount,
                        Currency = payment.Currency,
                        Last4 = payment.Last4
                    },
                    payment.PaymentId.ToString("N"),
                    cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish PaymentCompleted for {PaymentId}", payment.PaymentId);
            }
        }

        return failed ? UnprocessableEntity(payment) : Ok(payment);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public async Task<ActionResult<PaymentDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var payment = await _store.GetAsync(id, cancellationToken);
        return payment is null ? NotFound() : Ok(payment);
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((_, _, c) => c
                .WriteTo.Console()
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", "PaymentService"));

            builder.Services.AddMiniCommerceAzureCredential(builder.Configuration);
            builder.AddKeyVaultConfiguration();
            builder.AddMiniCommerceAksHosting();
            builder.Services.AddMiniCommerceOptions(builder.Configuration);
            builder.Services.AddMiniCommerceTelemetry(builder.Configuration);
            new ServiceBusServiceRegistrar().Register(builder.Services, builder.Configuration, registerConsumer: false);
            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);
            builder.Services.AddMiniCommerceHealthChecks(builder.Configuration);
            builder.Services.AddMiniCommerceCors(builder.Configuration);
            builder.Services.AddMiniCommerceDistributedCache(builder.Configuration);
            builder.Services.AddSingleton<IPaymentStore, DistributedPaymentStore>();

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
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapMiniCommerceHealthEndpoints();
            app.Run();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Payment Service terminated");
        }
        finally
        {
            Log.CloseAndFlush();
        }
    }
}
