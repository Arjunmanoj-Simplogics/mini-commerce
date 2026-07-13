namespace InventoryService.Application.DTOs;

public class InventoryItemDto
{
    public Guid Id { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}

public class CreateInventoryItemDto
{
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
}

public class UpdateInventoryItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
}
