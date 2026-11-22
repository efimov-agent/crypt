namespace SeanceScheduler.Web.Features.Seances;

/// <summary>
/// Input contract for POST /api/seances. Only checks shape (required values, order, name length);
/// duration and slot-grid rules live in <see cref="SeancesService"/>, which applies them on UTC values.
/// </summary>
public sealed record CreateSeanceRequest(int MediumId, string? ClientName, DateTime StartsAt, DateTime EndsAt)
{
    public const int ClientNameMaxLength = 100;
    public const int MinDurationMinutes = 30;
    public const int MaxDurationMinutes = 240;
    public const int DurationIncrementMinutes = 15;

    public Dictionary<string, string[]> Validate()
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(ClientName))
        {
            errors[nameof(ClientName)] = ["Client name is required."];
        }
        else if (ClientName.Trim().Length > ClientNameMaxLength)
        {
            errors[nameof(ClientName)] = [$"Client name must be at most {ClientNameMaxLength} characters."];
        }

        if (StartsAt == default)
        {
            errors[nameof(StartsAt)] = ["Start time is required."];
        }

        if (EndsAt == default)
        {
            errors[nameof(EndsAt)] = ["End time is required."];
        }

        if (StartsAt != default && EndsAt != default && StartsAt >= EndsAt)
        {
            errors[nameof(EndsAt)] = ["End time must be after start time."];
        }

        return errors;
    }
}