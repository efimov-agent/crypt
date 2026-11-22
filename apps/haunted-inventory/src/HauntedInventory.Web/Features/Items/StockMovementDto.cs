namespace HauntedInventory.Web.Features.Items;

/// <summary>
/// Response DTO for a stock movement
/// </summary>
public sealed record StockMovementDto(
    int Id,
    int ItemId,
    int Delta,
    string Reason,
    string? Note,
    int ResultingQuantity,
    DateTime RecordedAt)
{
    public static StockMovementDto FromEntity(Data.Models.StockMovement movement) => new(
        movement.Id,
        movement.ItemId,
        movement.Delta,
        movement.Reason,
        movement.Note,
        movement.ResultingQuantity,
        movement.RecordedAt);
}

/// <summary>
/// Response DTO for GET /api/items/{id}/movements
/// </summary>
public sealed record GetMovementsResponse(
    IReadOnlyList<StockMovementDto> Movements,
    int Total,
    int Limit,
    int Offset);