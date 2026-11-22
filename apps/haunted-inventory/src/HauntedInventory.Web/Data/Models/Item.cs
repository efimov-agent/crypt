namespace HauntedInventory.Web.Data.Models;

/// <summary>
/// Example entity for the haunted-inventory app - items in a rental shop
/// </summary>
public class Item
{
    public int Id { get; set; }
    
    public required string Name { get; set; }
    
    public string? Description { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    public DateTime? UpdatedAt { get; set; }
}