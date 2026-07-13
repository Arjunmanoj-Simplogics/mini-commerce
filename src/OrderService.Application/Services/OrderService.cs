using AutoMapper;
using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Events;
using MiniCommerce.Contracts.Inventory;
using OrderService.Application.DTOs;
using OrderService.Application.Exceptions;
using OrderService.Application.Interfaces;
using OrderService.Domain.Constants;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Application.Services;

/// <summary>
/// Implements order business logic and orchestrates Inventory + Notification integrations.
/// </summary>
public class OrderService : IOrderService
{
    private readonly IOrderRepository _orderRepository;
    private readonly IInventoryClient _inventoryClient;
    private readonly IIntegrationEventPublisher _eventPublisher;
    private readonly IMapper _mapper;
    private readonly ILogger<OrderService> _logger;

    public OrderService(
        IOrderRepository orderRepository,
        IInventoryClient inventoryClient,
        IIntegrationEventPublisher eventPublisher,
        IMapper mapper,
        ILogger<OrderService> logger)
    {
        _orderRepository = orderRepository;
        _inventoryClient = inventoryClient;
        _eventPublisher = eventPublisher;
        _mapper = mapper;
        _logger = logger;
    }

    public async Task<IReadOnlyList<OrderDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetAllAsync(cancellationToken);
        return _mapper.Map<IReadOnlyList<OrderDto>>(orders);
    }

    public async Task<IReadOnlyList<OrderDto>> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        var orders = await _orderRepository.GetByEmailAsync(email, cancellationToken);
        return _mapper.Map<IReadOnlyList<OrderDto>>(orders);
    }

    public async Task<OrderDto> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Order with id '{id}' was not found.");
        return _mapper.Map<OrderDto>(order);
    }

    public async Task<OrderDto> CreateAsync(CreateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var orderId = Guid.NewGuid();
        var sku = dto.ProductSku.Trim().ToUpperInvariant();

        // 1) Reserve inventory synchronously (saga step)
        var reserveResult = await _inventoryClient.ReserveStockAsync(new ReserveStockRequest
        {
            OrderId = orderId,
            ProductSku = sku,
            Quantity = dto.Quantity
        }, cancellationToken);

        if (!reserveResult.Success)
        {
            throw new ValidationException(reserveResult.Message);
        }

        var utcNow = DateTime.UtcNow;
        var order = new Order
        {
            Id = orderId,
            OrderNumber = GenerateOrderNumber(),
            CustomerName = dto.CustomerName.Trim(),
            Email = dto.Email.Trim(),
            ProductSku = sku,
            Quantity = dto.Quantity,
            TotalAmount = dto.TotalAmount,
            Status = OrderStatus.Pending,
            CreatedDate = utcNow,
            UpdatedDate = utcNow
        };

        try
        {
            var created = await _orderRepository.CreateAsync(order, cancellationToken);
            _logger.LogInformation("Order {OrderNumber} created with id {OrderId}", created.OrderNumber, created.Id);

            // 2) Notify asynchronously-style (fire HTTP event; failures are logged, order still succeeds)
            await _eventPublisher.PublishOrderCreatedAsync(new OrderCreatedEvent
            {
                OrderId = created.Id,
                OrderNumber = created.OrderNumber,
                CustomerName = created.CustomerName,
                Email = created.Email,
                ProductSku = created.ProductSku,
                Quantity = created.Quantity,
                TotalAmount = created.TotalAmount
            }, cancellationToken);

            return _mapper.Map<OrderDto>(created);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to create order; releasing reserved stock for {Sku}", sku);
            await SafeReleaseAsync(orderId, sku, dto.Quantity, cancellationToken);
            throw;
        }
    }

    public async Task<OrderDto> UpdateAsync(Guid id, UpdateOrderDto dto, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Order with id '{id}' was not found.");

        if (!Enum.TryParse<OrderStatus>(dto.Status, ignoreCase: true, out var status))
        {
            throw new ValidationException($"Invalid order status '{dto.Status}'.");
        }

        var previousStatus = order.Status;
        order.CustomerName = dto.CustomerName.Trim();
        order.Email = dto.Email.Trim();
        order.ProductSku = dto.ProductSku.Trim().ToUpperInvariant();
        order.Quantity = dto.Quantity;
        order.TotalAmount = dto.TotalAmount;
        order.Status = status;
        order.UpdatedDate = DateTime.UtcNow;

        await _orderRepository.UpdateAsync(order, cancellationToken);
        _logger.LogInformation("Order {OrderId} updated", id);

        if (previousStatus != status)
        {
            if (status == OrderStatus.Cancelled && previousStatus != OrderStatus.Cancelled)
            {
                await SafeReleaseAsync(order.Id, order.ProductSku, order.Quantity, cancellationToken);
                await _eventPublisher.PublishOrderCancelledAsync(new OrderCancelledEvent
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Email = order.Email,
                    ProductSku = order.ProductSku,
                    Quantity = order.Quantity
                }, cancellationToken);
            }
            else
            {
                await _eventPublisher.PublishOrderStatusChangedAsync(new OrderStatusChangedEvent
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    Email = order.Email,
                    CustomerName = order.CustomerName,
                    PreviousStatus = previousStatus.ToString(),
                    NewStatus = status.ToString()
                }, cancellationToken);
            }
        }

        return _mapper.Map<OrderDto>(order);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var order = await _orderRepository.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Order with id '{id}' was not found.");

        var deleted = await _orderRepository.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            throw new NotFoundException($"Order with id '{id}' was not found.");
        }

        if (order.Status != OrderStatus.Cancelled)
        {
            await SafeReleaseAsync(order.Id, order.ProductSku, order.Quantity, cancellationToken);
        }

        await _eventPublisher.PublishOrderCancelledAsync(new OrderCancelledEvent
        {
            OrderId = order.Id,
            OrderNumber = order.OrderNumber,
            Email = order.Email,
            ProductSku = order.ProductSku,
            Quantity = order.Quantity
        }, cancellationToken);

        _logger.LogInformation("Order {OrderId} deleted", id);
    }

    private async Task SafeReleaseAsync(Guid orderId, string sku, int quantity, CancellationToken cancellationToken)
    {
        try
        {
            await _inventoryClient.ReleaseStockAsync(new ReleaseStockRequest
            {
                OrderId = orderId,
                ProductSku = sku,
                Quantity = quantity
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to release stock for order {OrderId}, SKU {Sku}", orderId, sku);
        }
    }

    private static string GenerateOrderNumber()
        => $"{OrderConstants.OrderNumberPrefix}-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpperInvariant()}";
}
