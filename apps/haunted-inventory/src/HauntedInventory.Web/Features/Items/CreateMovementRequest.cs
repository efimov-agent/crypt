namespace HauntedInventory.Web.Features.Items;

/// <summary>
/// Input contract for POST /api/items/{id}/movements
/// </summary>
public sealed record CreateMovementRequest(int? Delta, string? Reason, string? Note)
{
    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (Delta == null)
        {
            errors[nameof(Delta)] = ["Delta is required."];
        }
        else if (Delta == 0)
        {
            errors[nameof(Delta)] = ["Delta must be non-zero."];
        }
        else if (Math.Abs((long)Delta.Value) > StockMovementsService.MaxAbsDelta)
        {
            errors[nameof(Delta)] = [$"Delta must be between -{StockMovementsService.MaxAbsDelta} and {StockMovementsService.MaxAbsDelta}."];
        }

        if (string.IsNullOrWhiteSpace(Reason))
        {
            errors[nameof(Reason)] = ["Reason is required."];
        }
        else if (!StockMovementsService.ValidReasons.Contains(Reason))
        {
            errors[nameof(Reason)] = [$"Reason must be one of: {string.Join(", ", StockMovementsService.ValidReasons)}"];
        }

        if (Note != null && Note.Length > StockMovementsService.NoteMaxLength)
        {
            errors[nameof(Note)] = [$"Note must be at most {StockMovementsService.NoteMaxLength} characters."];
        }

        return errors;
    }
}