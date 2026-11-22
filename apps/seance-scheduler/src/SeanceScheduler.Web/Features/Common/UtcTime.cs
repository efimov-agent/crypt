namespace SeanceScheduler.Web.Features.Common;

/// <summary>
/// Normalises DateTime values to UTC. Values without a kind (SQLite reads, form posts without offset)
/// are treated as UTC rather than server-local time, so behaviour does not depend on the host time zone.
/// </summary>
public static class UtcTime
{
    public static DateTime Normalize(DateTime value) => value.Kind switch
    {
        DateTimeKind.Utc => value,
        DateTimeKind.Local => value.ToUniversalTime(),
        _ => DateTime.SpecifyKind(value, DateTimeKind.Utc),
    };

    public static DateTime? Normalize(DateTime? value) => value.HasValue ? Normalize(value.Value) : null;

    /// <summary>True when the value lies exactly on a slot boundary (no seconds or sub-second ticks).</summary>
    public static bool IsOnBoundary(DateTime value, int minutes) =>
        value.Ticks % TimeSpan.FromMinutes(minutes).Ticks == 0;
}
