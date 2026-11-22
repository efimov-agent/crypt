namespace SeanceScheduler.Web.Features.Seances;

/// <summary>
/// DTO for returning Seance data
/// </summary>
public sealed record SeanceDto(int Id, int MediumId, string ClientName, DateTime StartsAt, DateTime EndsAt, string Status, DateTime CreatedAt, DateTime? CancelledAt);