using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HauntedInventory.Web.Features.Items;

public static class ItemsEndpoints
{
    public static void MapItemsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/items");

        group.MapGet("/", GetItems);
        group.MapGet("/{id:int}", GetItem);
        group.MapPost("/", CreateItem);
        group.MapDelete("/{id:int}", DeleteItem);

        // Stock movement endpoints
        group.MapPost("/{id:int}/movements", RecordMovement);
        group.MapGet("/{id:int}/movements", GetMovements);
    }

    private static async Task<IResult> GetItems(ItemsService service, ILogger<ItemsService> logger, [FromQuery] bool lowStock = false, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("GET /api/items requested (lowStock={LowStock})", lowStock);

        var items = await service.GetItemsAsync(lowStockOnly: lowStock, cancellationToken: cancellationToken);
        var dtos = items.Select(ItemDto.FromEntity).ToList();

        logger.LogInformation("Returning {Count} items", dtos.Count);
        return Results.Ok(dtos);
    }

    private static async Task<IResult> GetItem(int id, ItemsService service, ILogger<ItemsService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("GET /api/items/{Id} requested", id);

        var item = await service.GetItemByIdAsync(id, cancellationToken);

        if (item == null)
        {
            logger.LogWarning("Item {Id} not found", id);
            return Results.NotFound(new { message = $"Item {id} not found" });
        }

        return Results.Ok(ItemDto.FromEntity(item));
    }

    private static async Task<IResult> CreateItem(CreateItemRequest request, ItemsService service, ILogger<ItemsService> logger, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var normalizedSku = request.GetNormalizedSku()!;

        // Check if SKU already exists
        if (await service.SkuExistsAsync(normalizedSku, cancellationToken))
        {
            logger.LogWarning("Attempted to create item with duplicate SKU: {Sku}", normalizedSku);
            return Results.Conflict(new ProblemDetails
            {
                Type = "about:blank",
                Title = "Conflict",
                Status = 409,
                Detail = "SKU already in use",
                Instance = "/api/items"
            });
        }

        try
        {
            var created = await service.CreateItemAsync(
                request.Name!.Trim(),
                string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(),
                normalizedSku,
                request.ReorderLevel ?? 0,
                cancellationToken);

            logger.LogInformation("Created item {Id} with SKU {Sku}", created.Id, created.Sku);
            return Results.Created($"/api/items/{created.Id}", ItemDto.FromEntity(created));
        }
        catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique") == true ||
                                           ex.InnerException?.Message.Contains("UNIQUE constraint failed") == true)
        {
            logger.LogWarning(ex, "SKU constraint violation for SKU: {Sku}", normalizedSku);
            return Results.Conflict(new ProblemDetails
            {
                Type = "about:blank",
                Title = "Conflict",
                Status = 409,
                Detail = "SKU already in use",
                Instance = "/api/items"
            });
        }
    }

    private static async Task<IResult> DeleteItem(int id, ItemsService service, ILogger<ItemsService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("DELETE /api/items/{Id} requested", id);

        var deleted = await service.DeleteItemAsync(id, cancellationToken);

        if (!deleted)
        {
            logger.LogWarning("Item {Id} not found for deletion", id);
            return Results.NotFound(new { message = $"Item {id} not found" });
        }

        logger.LogInformation("Deleted item {Id}", id);
        return Results.NoContent();
    }

    private static async Task<IResult> RecordMovement(int id, CreateMovementRequest request, StockMovementsService service, ILogger<StockMovementsService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("POST /api/items/{Id}/movements requested", id);

        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        try
        {
            var movement = await service.RecordMovementAsync(
                id,
                request.Delta!.Value,
                request.Reason!.Trim(),
                request.Note,
                cancellationToken);

            logger.LogInformation("Recorded movement {MovementId} for item {ItemId}", movement.Id, id);
            return Results.Created($"/api/items/{id}/movements/{movement.Id}", StockMovementDto.FromEntity(movement));
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Invalid movement request for item {ItemId}", id);
            return Results.BadRequest(new ProblemDetails
            {
                Type = "about:blank",
                Title = "Bad Request",
                Status = 400,
                Detail = ex.Message,
                Instance = $"/api/items/{id}/movements"
            });
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Item {Id} not found for movement", id);
            return Results.NotFound(new { message = $"Item {id} not found" });
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Movement would result in invalid quantity for item {ItemId}", id);
            return Results.Conflict(new ProblemDetails
            {
                Type = "about:blank",
                Title = "Conflict",
                Status = 409,
                Detail = ex.Message,
                Instance = $"/api/items/{id}/movements"
            });
        }
    }

    private static async Task<IResult> GetMovements(int id, [FromQuery] string? reason = null, [FromQuery] int limit = 100, [FromQuery] int offset = 0, StockMovementsService service = null!, ILogger<StockMovementsService> logger = null!, CancellationToken cancellationToken = default)
    {
        logger.LogInformation("GET /api/items/{Id}/movements requested (reason={Reason}, limit={Limit}, offset={Offset})",
            id, reason, limit, offset);

        // Parse reason filter
        string[]? reasonFilter = null;
        if (!string.IsNullOrWhiteSpace(reason))
        {
            reasonFilter = reason.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(r => r.Trim())
                .ToArray();
        }

        try
        {
            var movements = await service.GetMovementsAsync(id, reasonFilter, limit, offset, cancellationToken);
            var dtos = movements.Select(StockMovementDto.FromEntity).ToList();

            logger.LogInformation("Returning {Count} movements for item {ItemId}", dtos.Count, id);
            return Results.Ok(new GetMovementsResponse(dtos, dtos.Count, limit, offset));
        }
        catch (KeyNotFoundException)
        {
            logger.LogWarning("Item {Id} not found for movements", id);
            return Results.NotFound(new { message = $"Item {id} not found" });
        }
    }
}