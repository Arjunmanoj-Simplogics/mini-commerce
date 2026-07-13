using AutoMapper;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Inventory;
using Moq;
using OrderService.Application.DTOs;
using OrderService.Application.Exceptions;
using OrderService.Application.Interfaces;
using OrderService.Application.Mappings;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;

namespace OrderService.Tests;

public class OrderServiceTests
{
    private readonly Mock<IOrderRepository> _repositoryMock = new();
    private readonly Mock<IInventoryClient> _inventoryMock = new();
    private readonly Mock<IIntegrationEventPublisher> _publisherMock = new();
    private readonly Mock<ILogger<OrderService.Application.Services.OrderService>> _loggerMock = new();
    private readonly OrderService.Application.Services.OrderService _sut;

    public OrderServiceTests()
    {
        var mapper = new MapperConfiguration(cfg => cfg.AddProfile<OrderProfile>()).CreateMapper();
        _sut = new OrderService.Application.Services.OrderService(
            _repositoryMock.Object,
            _inventoryMock.Object,
            _publisherMock.Object,
            mapper,
            _loggerMock.Object);

        _inventoryMock
            .Setup(x => x.ReserveStockAsync(It.IsAny<ReserveStockRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ReserveStockResponse { Success = true, Message = "ok", RemainingQuantity = 10 });
    }

    [Fact]
    public async Task GetAllAsync_ReturnsMappedOrders()
    {
        _repositoryMock.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Order> { CreateSampleOrder() });

        var result = await _sut.GetAllAsync();

        result.Should().HaveCount(1);
        result[0].ProductSku.Should().Be("SKU-LAPTOP-01");
    }

    [Fact]
    public async Task CreateAsync_ReservesInventoryAndPublishesEvent()
    {
        _repositoryMock
            .Setup(r => r.CreateAsync(It.IsAny<Order>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Order order, CancellationToken _) => order);

        var result = await _sut.CreateAsync(new CreateOrderDto
        {
            CustomerName = "Alice",
            Email = "alice@example.com",
            ProductSku = "SKU-PHONE-01",
            Quantity = 2,
            TotalAmount = 120m
        });

        result.ProductSku.Should().Be("SKU-PHONE-01");
        result.Quantity.Should().Be(2);
        _inventoryMock.Verify(x => x.ReserveStockAsync(It.IsAny<ReserveStockRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.PublishOrderCreatedAsync(It.IsAny<MiniCommerce.Contracts.Events.OrderCreatedEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenOrderMissing_ThrowsNotFoundException()
    {
        var id = Guid.NewGuid();
        _repositoryMock.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((Order?)null);

        var act = () => _sut.GetByIdAsync(id);
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ReleasesStock()
    {
        var order = CreateSampleOrder();
        _repositoryMock.Setup(r => r.GetByIdAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(order);
        _repositoryMock.Setup(r => r.DeleteAsync(order.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);

        await _sut.DeleteAsync(order.Id);

        _inventoryMock.Verify(x => x.ReleaseStockAsync(It.IsAny<ReleaseStockRequest>(), It.IsAny<CancellationToken>()), Times.Once);
        _publisherMock.Verify(x => x.PublishOrderCancelledAsync(It.IsAny<MiniCommerce.Contracts.Events.OrderCancelledEvent>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    private static Order CreateSampleOrder()
    {
        var utcNow = DateTime.UtcNow;
        return new Order
        {
            Id = Guid.NewGuid(),
            OrderNumber = "ORD-TEST-001",
            CustomerName = "Test User",
            Email = "test@example.com",
            ProductSku = "SKU-LAPTOP-01",
            Quantity = 1,
            TotalAmount = 99.99m,
            Status = OrderStatus.Pending,
            CreatedDate = utcNow,
            UpdatedDate = utcNow
        };
    }
}
