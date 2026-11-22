namespace HauntedInventory.Web.Data.Models;

/// <summary>
/// Tracks a single stock movement for an item.
/// </summary>
public class StockMovement
{
    public int Id { get; set; }

    public int ItemId { get; set; }

    public Item Item { get; set; } = null!;

    public required int Delta { get; set; }

    public required string Reason { get; set; }

    public string? Note { get; set; }

    public int ResultingQuantity { get; set; }

    public DateTime RecordedAt { get; set; } = DateTime.UtcNow;
}