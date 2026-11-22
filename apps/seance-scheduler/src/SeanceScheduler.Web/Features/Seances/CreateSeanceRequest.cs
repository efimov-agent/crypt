namespace SeanceScheduler.Web.Features.Seances;

/// <summary>
/// Input contract for POST /api/seances
/// </summary>
public sealed record CreateSeanceRequest(int MediumId, string? ClientName, DateTime StartsAt, DateTime EndsAt)
{
    public const int ClientNameMaxLength = 100;

    /// <summary>All request errors at once: shape checks plus <see cref="SeanceSlotRules"/> when both times are present.</summary>
    public Dictionary<string, string[]> Validate(DateTime nowUtc)
    {
        var failures = new List<(string Field, string Message)>();

        if (string.IsNullOrWhiteSpace(ClientName))
        {
            failures.Add((nameof(ClientName), "Client name is required."));
        }
        else if (ClientName.Trim().Length > ClientNameMaxLength)
        {
            failures.Add((nameof(ClientName), $"Client name must be at most {ClientNameMaxLength} characters."));
        }

        if (StartsAt == default)
        {
            failures.Add((nameof(StartsAt), "Start time is required."));
        }

        if (EndsAt == default)
        {
            failures.Add((nameof(EndsAt), "End time is required."));
        }

        if (StartsAt != default && EndsAt != default)
        {
            failures.AddRange(SeanceSlotRules.Check(StartsAt, EndsAt, nowUtc));
        }

        return SeanceSlotRules.ToErrors(failures);
    }
}
