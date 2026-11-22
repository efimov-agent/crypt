using HauntedInventory.Web.Data;
using HauntedInventory.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HauntedInventory.Web.Features.Items;

public class ItemsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ItemsService> _logger;

    public ItemsService(ApplicationDbContext dbContext, ILogger<ItemsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Item>> GetItemsAsync(bool lowStockOnly = false, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Fetching all items at {Time}", DateTime.UtcNow);

        var query = _dbContext.Items.AsQueryable();

        if (lowStockOnly)
        {
            query = query.Where(i => i.Quantity <= i.ReorderLevel);
        }

        return await query
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Item?> GetItemByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching item {ItemId} at {Time}", id, DateTime.UtcNow);

        return await _dbContext.Items.FindAsync([id], cancellationToken);
    }

    public async Task<Item> CreateItemAsync(string name, string? description, string sku, int reorderLevel = 0, CancellationToken cancellationToken = default)
    {
        var item = new Item
        {
            Name = name,
            Description = description,
            Sku = sku,
            Quantity = 0, // Stock only changes through movements
            ReorderLevel = reorderLevel,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Items.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created item {ItemId} - {ItemName} with SKU {Sku}", item.Id, item.Name, item.Sku);
        return item;
    }

    public async Task<bool> DeleteItemAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deleting item {ItemId} at {Time}", id, DateTime.UtcNow);

        var item = await _dbContext.Items.FindAsync([id], cancellationToken);
        if (item == null)
        {
            _logger.LogWarning("Item {ItemId} not found for deletion", id);
            return false;
        }

        _dbContext.Items.Remove(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deleted item {ItemId}", id);
        return true;
    }

    public async Task<bool> SkuExistsAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Items.AnyAsync(i => i.Sku == sku, cancellationToken);
    }

    public async Task<Item?> GetItemBySkuAsync(string sku, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Items.FirstOrDefaultAsync(i => i.Sku == sku, cancellationToken);
    }
}