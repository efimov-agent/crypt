namespace SeanceScheduler.Web.Features.Items;

public static class ItemsEndpoints
{
    public static void MapItemsEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/items");
        
        group.MapGet("/", GetItems);
        group.MapGet("/{id:int}", GetItem);
        group.MapPost("/", CreateItem);
        group.MapDelete("/{id:int}", DeleteItem);
    }

    private static async Task<IResult> GetItems(ItemsService service, ILogger<ItemsService> logger, CancellationToken cancellationToken)
    {
        logger.LogInformation("GET /api/items requested");
        
        var items = await service.GetItemsAsync(cancellationToken);
        
        logger.LogInformation("Returning {Count} items", items.Count);
        return Results.Ok(items);
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
        
        return Results.Ok(item);
    }

    private static async Task<IResult> CreateItem(CreateItemRequest request, ItemsService service, ILogger<ItemsService> logger, CancellationToken cancellationToken)
    {
        var errors = request.Validate();
        if (errors.Count > 0)
        {
            return Results.ValidationProblem(errors);
        }

        var created = await service.CreateItemAsync(request.Name!.Trim(), string.IsNullOrWhiteSpace(request.Description) ? null : request.Description.Trim(), cancellationToken);

        logger.LogInformation("Created item {Id}", created.Id);
        return Results.Created($"/api/items/{created.Id}", created);
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
}