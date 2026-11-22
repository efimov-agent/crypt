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
    public const int MaxAbsDelta = 1_000_000;

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

        if (Math.Abs((long)delta) > MaxAbsDelta)
        {
            throw new ArgumentException($"Delta must be between -{MaxAbsDelta} and {MaxAbsDelta}.", nameof(delta));
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

        // Disposing the transaction without Commit rolls it back (also on cancellation or errors).
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
            // Query the database directly: ExecuteUpdate bypasses the change tracker,
            // so an Item already tracked in this scope would hold a stale Quantity.
            var item = await _dbContext.Items.AsNoTracking()
                .Where(i => i.Id == itemId)
                .Select(i => new { i.Quantity })
                .FirstOrDefaultAsync(cancellationToken);
            if (item == null)
            {
                throw new KeyNotFoundException($"Item {itemId} not found.");
            }

            // Item exists but quantity constraint failed
            _logger.LogWarning("Movement would result in negative quantity for item {ItemId}. Current: {Current}, Delta: {Delta}",
                itemId, item.Quantity, delta);
            throw new InsufficientStockException(
                $"Stock movement would result in negative quantity. Current: {item.Quantity}, Delta: {delta}");
        }

        // Read the new quantity from the database (not the change tracker, see above)
        var resultingQuantity = await _dbContext.Items.AsNoTracking()
            .Where(i => i.Id == itemId)
            .Select(i => i.Quantity)
            .SingleAsync(cancellationToken);

        // Create movement record with resulting quantity
        var movement = new StockMovement
        {
            ItemId = itemId,
            Delta = delta,
            Reason = reason,
            Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim(),
            ResultingQuantity = resultingQuantity,
            RecordedAt = DateTime.UtcNow
        };

        _dbContext.StockMovements.Add(movement);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Recorded movement for item {ItemId}: Delta={Delta}, Reason={Reason}, ResultingQuantity={ResultingQuantity}",
            itemId, delta, reason, movement.ResultingQuantity);

        return movement;
    }

    public const int DefaultPageSize = 100;
    public const int MaxPageSize = 1000;

    /// <summary>
    /// Returns one page of an item's movement history, newest first (ties broken by Id),
    /// together with the total number of matching movements and the limit/offset actually applied.
    /// </summary>
    /// <exception cref="KeyNotFoundException">The item does not exist.</exception>
    /// <exception cref="ArgumentException">The reason filter contains an unknown reason.</exception>
    public async Task<MovementPage> GetMovementsAsync(
        int itemId,
        string[]? reasonFilter = null,
        int limit = DefaultPageSize,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        limit = limit < 1 ? DefaultPageSize : Math.Min(limit, MaxPageSize);
        offset = Math.Max(offset, 0);

        if (reasonFilter is { Length: > 0 })
        {
            var unknown = reasonFilter.Where(r => !ValidReasons.Contains(r)).ToArray();
            if (unknown.Length > 0)
            {
                throw new ArgumentException(
                    $"Unknown reason(s): {string.Join(", ", unknown)}. Allowed: {string.Join(", ", ValidReasons)}", nameof(reasonFilter));
            }
        }

        if (!await _dbContext.Items.AnyAsync(i => i.Id == itemId, cancellationToken))
        {
            throw new KeyNotFoundException($"Item {itemId} not found.");
        }

        var query = _dbContext.StockMovements.AsNoTracking().Where(m => m.ItemId == itemId);
        if (reasonFilter is { Length: > 0 })
        {
            query = query.Where(m => reasonFilter.Contains(m.Reason));
        }

        var total = await query.CountAsync(cancellationToken);
        var movements = await query
            .OrderByDescending(m => m.RecordedAt)
            .ThenByDescending(m => m.Id)
            .Skip(offset)
            .Take(limit)
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Retrieved {Count} of {Total} movements for item {ItemId}", movements.Count, total, itemId);
        return new MovementPage(movements, total, limit, offset);
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

/// <summary>One page of movement history plus paging metadata.</summary>
public sealed record MovementPage(IReadOnlyList<StockMovement> Movements, int Total, int Limit, int Offset);

/// <summary>Thrown when a movement would drive an item's quantity below zero.</summary>
public sealed class InsufficientStockException(string message) : Exception(message);
