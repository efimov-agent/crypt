namespace SeanceScheduler.Web.Features.Seances;

/// <summary>
/// Input contract for POST /api/seances
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

        if (StartsAt != default && EndsAt != default && StartsAt < EndsAt)
        {
            var duration = (EndsAt - StartsAt).TotalMinutes;
            if (duration < MinDurationMinutes)
            {
                errors[nameof(EndsAt)] = [$"Duration must be at least {MinDurationMinutes} minutes."];
            }
            else if (duration > MaxDurationMinutes)
            {
                errors[nameof(EndsAt)] = [$"Duration must be at most {MaxDurationMinutes} minutes."];
            }
            else if (duration % DurationIncrementMinutes != 0)
            {
                errors[nameof(EndsAt)] = [$"Duration must be a multiple of {DurationIncrementMinutes} minutes."];
            }

            // Check 15-minute boundary for StartsAt
            if (!Common.UtcTime.IsOnBoundary(StartsAt, DurationIncrementMinutes))
            {
                errors[nameof(StartsAt)] = ["Start time must be on a 15-minute boundary (e.g., 14:00, 14:15, 14:30, 14:45)."];
            }
        }

        return errors;
    }
}