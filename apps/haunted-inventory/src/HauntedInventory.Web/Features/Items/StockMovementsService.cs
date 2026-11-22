using HauntedInventory.Web.Data;
using HauntedInventory.Web.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace HauntedInventory.Web.Features.Items;

public class StockMovementsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<StockMovementsService> _logger;

    public static readonly string[] ValidReasons = ["Received", "Sold", "Damaged", "Adjustment"];
    public const int NoteMaxLength = 200;

    public StockMovementsService(ApplicationDbContext dbContext, ILogger<StockMovementsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<StockMovement> RecordMovementAsync(
        int itemId,
        int delta,
        string reason,
        string? note,
        CancellationToken cancellationToken)
    {
        // Validate delta is not zero
        if (delta == 0)
        {
            _logger.LogWarning("Attempted to record movement with delta=0 for item {ItemId}", itemId);
            throw new ArgumentException("Delta must be non-zero.", nameof(delta));
        }

        // Validate reason is valid
        if (!ValidReasons.Contains(reason))
        {
            _logger.LogWarning("Invalid reason '{Reason}' for item {ItemId}", reason, itemId);
            throw new ArgumentException($"Reason must be one of: {string.Join(", ", ValidReasons)}", nameof(reason));
        }

        // Validate note length
        if (note != null && note.Length > NoteMaxLength)
        {
            _logger.LogWarning("Note too long for item {ItemId}: {Length} chars", itemId, note.Length);
            throw new ArgumentException($"Note must be at most {NoteMaxLength} characters.", nameof(note));
        }

        // Use a transaction for atomic update
        using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            // Atomic update with compound WHERE clause to prevent negative quantity
            var rowsAffected = await _dbContext.Items
                .Where(i => i.Id == itemId && i.Quantity + delta >= 0)
                .ExecuteUpdateAsync(setters => setters
                    .SetProperty(i => i.Quantity, i => i.Quantity + delta)
                    .SetProperty(i => i.UpdatedAt, DateTime.UtcNow),
                    cancellationToken);

            if (rowsAffected == 0)
            {
                // Check if item exists to distinguish between not found vs insufficient stock
                var item = await _dbContext.Items.FindAsync([itemId], cancellationToken);
                if (item == null)
                {
                    throw new KeyNotFoundException($"Item {itemId} not found.");
                }

                // Item exists but quantity constraint failed
                _logger.LogWarning("Movement would result in negative quantity for item {ItemId}. Current: {Current}, Delta: {Delta}",
                    itemId, item.Quantity, delta);
                throw new InvalidOperationException(
                    $"Stock movement would result in negative quantity. Current: {item.Quantity}, Delta: {delta}");
            }

            // Get the updated item to read the new quantity
            var updatedItem = await _dbContext.Items.FindAsync([itemId], cancellationToken);

            // Create movement record with resulting quantity
            var movement = new StockMovement
            {
                ItemId = itemId,
                Delta = delta,
                Reason = reason,
                Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
                ResultingQuantity = updatedItem!.Quantity,
                RecordedAt = DateTime.UtcNow
            };

            _dbContext.StockMovements.Add(movement);
            await _dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            _logger.LogInformation("Recorded movement for item {ItemId}: Delta={Delta}, Reason={Reason}, ResultingQuantity={ResultingQuantity}",
                itemId, delta, reason, movement.ResultingQuantity);

            return movement;
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    public async Task<IReadOnlyList<StockMovement>> GetMovementsAsync(
        int itemId,
        string[]? reasonFilter = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        // Validate limit
        if (limit > 1000)
        {
            limit = 1000;
        }
        else if (limit < 1)
        {
            limit = 100;
        }

        // Validate offset
        if (offset < 0)
        {
            offset = 0;
        }

        // Check if item exists
        var item = await _dbContext.Items.FindAsync([itemId], cancellationToken);
        if (item == null)
        {
            throw new KeyNotFoundException($"Item {itemId} not found.");
        }

        var query = _dbContext.StockMovements
            .Where(m => m.ItemId == itemId)
            .AsQueryable();

        // Apply reason filter if provided
        if (reasonFilter != null && reasonFilter.Length > 0)
        {
            var validFilters = reasonFilter.Where(r => ValidReasons.Contains(r)).ToArray();
            if (validFilters.Length > 0)
            {
                query = query.Where(m => validFilters.Contains(m.Reason));
            }
        }

        var movements = await query
            .OrderByDescending(m => m.RecordedAt)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} movements for item {ItemId}", movements.Count, itemId);
        return movements;
    }

    public async Task<IReadOnlyList<Item>> GetLowStockItemsAsync(CancellationToken cancellationToken)
    {
        var items = await _dbContext.Items
            .Where(i => i.Quantity <= i.ReorderLevel)
            .OrderBy(i => i.Name)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} low stock items", items.Count);
        return items;
    }
}