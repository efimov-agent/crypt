namespace HauntedInventory.Web.Features.Items;

/// <summary>
/// Response DTO for an item
/// </summary>
public sealed record ItemDto(
    int Id,
    string Name,
    string? Description,
    string Sku,
    int Quantity,
    int ReorderLevel,
    DateTime CreatedAt,
    DateTime? UpdatedAt)
{
    public static ItemDto FromEntity(Data.Models.Item item) => new(
        item.Id,
        item.Name,
        item.Description,
        item.Sku,
        item.Quantity,
        item.ReorderLevel,
        item.CreatedAt,
        item.UpdatedAt);
}