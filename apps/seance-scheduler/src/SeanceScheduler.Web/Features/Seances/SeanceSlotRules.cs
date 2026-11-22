using SeanceScheduler.Web.Data.Models;
using SeanceScheduler.Web.Features.Common;

namespace SeanceScheduler.Web.Features.Seances;

/// <summary>
/// The single implementation of the booking slot rules (duration, 15-minute grid, future start/end).
/// Values are normalised to UTC first, so the result does not depend on the caller's offset or the host time zone.
/// </summary>
public static class SeanceSlotRules
{
    public const int MinDurationMinutes = 30;
    public const int MaxDurationMinutes = 240;
    public const int DurationIncrementMinutes = 15;

    public static IEnumerable<(string Field, string Message)> Check(DateTime startsAt, DateTime endsAt, DateTime nowUtc)
    {
        var startsAtUtc = UtcTime.Normalize(startsAt);
        var endsAtUtc = UtcTime.Normalize(endsAt);

        var duration = (endsAtUtc - startsAtUtc).TotalMinutes;
        if (duration <= 0)
        {
            yield return (nameof(Seance.EndsAt), "End time must be after start time.");
        }
        else if (duration < MinDurationMinutes)
        {
            yield return (nameof(Seance.EndsAt), $"Duration must be at least {MinDurationMinutes} minutes.");
        }
        else if (duration > MaxDurationMinutes)
        {
            yield return (nameof(Seance.EndsAt), $"Duration must be at most {MaxDurationMinutes} minutes.");
        }
        else if (duration % DurationIncrementMinutes != 0)
        {
            yield return (nameof(Seance.EndsAt), $"Duration must be a multiple of {DurationIncrementMinutes} minutes.");
        }

        if (!UtcTime.IsOnBoundary(startsAtUtc, DurationIncrementMinutes))
        {
            yield return (nameof(Seance.StartsAt), "Start time must be on a 15-minute boundary (e.g., 14:00, 14:15, 14:30, 14:45).");
        }

        if (startsAtUtc <= nowUtc)
        {
            yield return (nameof(Seance.StartsAt), "Start time must be in the future.");
        }

        if (endsAtUtc <= nowUtc)
        {
            yield return (nameof(Seance.EndsAt), "End time must be in the future.");
        }
    }

    public static Dictionary<string, string[]> ToErrors(IEnumerable<(string Field, string Message)> failures) =>
        failures.GroupBy(f => f.Field).ToDictionary(g => g.Key, g => g.Select(f => f.Message).ToArray());
}
