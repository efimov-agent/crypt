using SeanceScheduler.Web.Data;
using SeanceScheduler.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace SeanceScheduler.Web.Features.Items;

public class ItemsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<ItemsService> _logger;

    public ItemsService(ApplicationDbContext dbContext, ILogger<ItemsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<Item>> GetItemsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all items at {Time}", DateTime.UtcNow);
        
        return await _dbContext.Items
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<Item?> GetItemByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching item {ItemId} at {Time}", id, DateTime.UtcNow);
        
        return await _dbContext.Items.FindAsync([id], cancellationToken);
    }

    public async Task<Item> CreateItemAsync(string name, string? description, CancellationToken cancellationToken)
    {
        var item = new Item
        {
            Name = name,
            Description = description,
            CreatedAt = DateTime.UtcNow
        };
        _dbContext.Items.Add(item);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created item {ItemId} - {ItemName}", item.Id, item.Name);
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
}