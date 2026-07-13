using AutoMapper;
using FluentAssertions;
using InventoryService.Application.Interfaces;
using InventoryService.Application.Services;
using InventoryService.Domain.Entities;
using Microsoft.Extensions.Logging;
using MiniCommerce.Contracts.Inventory;
using Moq;

namespace InventoryService.Tests;

public class InventoryServiceTests
{
    [Fact]
    public async Task ReserveAsync_WhenStockAvailable_DecrementsAvailable()
    {
        var item = new InventoryItem
        {
            Id = Guid.NewGuid(),
            ProductSku = "SKU-PHONE-01",
            ProductName = "Phone",
            QuantityAvailable = 10,
            QuantityReserved = 0,
            CreatedDate = DateTime.UtcNow,
            UpdatedDate = DateTime.UtcNow
        };

        var repo = new Mock<IInventoryRepository>();
        repo.Setup(r => r.GetBySkuAsync("SKU-PHONE-01", It.IsAny<CancellationToken>())).ReturnsAsync(item);

        var mapper = new MapperConfiguration(cfg => cfg.CreateMap<InventoryItem, InventoryService.Application.DTOs.InventoryItemDto>()).CreateMapper();
        var sut = new InventoryService.Application.Services.InventoryService(
            repo.Object, mapper, Mock.Of<ILogger<InventoryService.Application.Services.InventoryService>>());

        var result = await sut.ReserveAsync(new ReserveStockRequest
        {
            ProductSku = "SKU-PHONE-01",
            Quantity = 3,
            OrderId = Guid.NewGuid()
        });

        result.Success.Should().BeTrue();
        item.QuantityAvailable.Should().Be(7);
        item.QuantityReserved.Should().Be(3);
    }
}
