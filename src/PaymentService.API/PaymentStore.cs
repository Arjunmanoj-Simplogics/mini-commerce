using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;

namespace PaymentService.API;

/// <summary>
/// Cross-replica payment lookup. Uses <see cref="IDistributedCache"/> so multiple pods share state
/// when Redis (or another distributed cache) is configured; falls back to in-process memory otherwise.
/// Charge / GET API contracts unchanged.
/// </summary>
public interface IPaymentStore
{
    Task SaveAsync(PaymentDto payment, CancellationToken cancellationToken = default);
    Task<PaymentDto?> GetAsync(Guid paymentId, CancellationToken cancellationToken = default);
}

/// <inheritdoc />
public sealed class DistributedPaymentStore : IPaymentStore
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly IDistributedCache _cache;

    public DistributedPaymentStore(IDistributedCache cache)
    {
        _cache = cache;
    }

    public async Task SaveAsync(PaymentDto payment, CancellationToken cancellationToken = default)
    {
        var bytes = JsonSerializer.SerializeToUtf8Bytes(payment, JsonOptions);
        await _cache.SetAsync(
            Key(payment.PaymentId),
            bytes,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24)
            },
            cancellationToken);
    }

    public async Task<PaymentDto?> GetAsync(Guid paymentId, CancellationToken cancellationToken = default)
    {
        var bytes = await _cache.GetAsync(Key(paymentId), cancellationToken);
        if (bytes is null || bytes.Length == 0)
        {
            return null;
        }

        return JsonSerializer.Deserialize<PaymentDto>(bytes, JsonOptions);
    }

    private static string Key(Guid id) => $"payment:{id:N}";
}
