namespace HauntedInventory.Web.Data.Models;

/// <summary>
/// Example entity for the haunted-inventory app - items in a rental shop
/// </summary>
public class Item
{
    public int Id { get; set; }

    public required string Name { get; set; }

    public string? Description { get; set; }

    public string Sku { get; set; } = string.Empty;

    public int Quantity { get; set; } = 0;

    public int ReorderLevel { get; set; } = 0;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAt { get; set; }

    public ICollection<StockMovement> Movements { get; set; } = [];
}