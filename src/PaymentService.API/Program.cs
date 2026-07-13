using System.Collections.Concurrent;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiniCommerce.BuildingBlocks.Auth;
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
    private static readonly ConcurrentDictionary<Guid, PaymentDto> Store = new();

    /// <summary>
    /// Mock charge. Cards ending in 0000 fail; otherwise succeeds.
    /// </summary>
    [HttpPost("charge")]
    [Authorize]
    public ActionResult<PaymentDto> Charge([FromBody] ChargeRequest request)
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

        Store[payment.PaymentId] = payment;
        return failed ? UnprocessableEntity(payment) : Ok(payment);
    }

    [HttpGet("{id:guid}")]
    [Authorize]
    public ActionResult<PaymentDto> GetById(Guid id)
        => Store.TryGetValue(id, out var payment) ? Ok(payment) : NotFound();
}

public class Program
{
    public static void Main(string[] args)
    {
        Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();
        try
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Host.UseSerilog((_, _, c) => c.WriteTo.Console());

            builder.Services.AddControllers();
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddMiniCommerceJwtAuthentication(builder.Configuration);
            builder.Services.AddHealthChecks()
                .AddCheck("self", () => Microsoft.Extensions.Diagnostics.HealthChecks.HealthCheckResult.Healthy(), tags: ["live", "ready"]);

            var origins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>() ?? ["http://localhost:5173"];
            builder.Services.AddCors(o => o.AddPolicy("FrontendPolicy", p => p.WithOrigins(origins).AllowAnyHeader().AllowAnyMethod()));

            var app = builder.Build();
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseCors("FrontendPolicy");
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapControllers();
            app.MapHealthChecks("/api/health");
            app.MapHealthChecks("/api/health/live", new() { Predicate = c => c.Tags.Contains("live") });
            app.MapHealthChecks("/api/health/ready", new() { Predicate = c => c.Tags.Contains("ready") });
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
