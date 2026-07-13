using OrderService.Application.DTOs;

namespace OrderService.Application.Interfaces;

/// <summary>
/// Defines order business operations.
/// </summary>
public interface IOrderService
{
    /// <summary>
    /// Retrieves all orders.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of order DTOs.</returns>
    Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<OrderDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves an order by identifier.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The order DTO.</returns>
    Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Creates a new order.
    /// </summary>
    /// <param name="dto">The create order DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created order DTO.</returns>
    Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing order.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="dto">The update order DTO.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The updated order DTO.</returns>
    Task<OrderDto> UpdateAsync(Guid id, UpdateOrderDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes an order.
    /// </summary>
    /// <param name="id">The order identifier.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
