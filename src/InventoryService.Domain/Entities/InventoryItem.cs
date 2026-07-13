namespace InventoryService.Domain.Entities;

/// <summary>
/// Represents a product stock record.
/// </summary>
public class InventoryItem
{
    public Guid Id { get; set; }
    public string ProductSku { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int QuantityAvailable { get; set; }
    public int QuantityReserved { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime UpdatedDate { get; set; }
}
