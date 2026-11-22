using SeanceScheduler.Web.Data;
using SeanceScheduler.Web.Data.Models;
using SeanceScheduler.Web.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace SeanceScheduler.Web.Features.Seances;

public class SeancesService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<SeancesService> _logger;

    public SeancesService(ApplicationDbContext dbContext, TimeProvider timeProvider, ILogger<SeancesService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<SeanceDto>> GetSeancesAsync(int? mediumId, DateTime? from, DateTime? to, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching seances at {Time}. Filters: mediumId={MediumId}, from={From}, to={To}",
            DateTime.UtcNow, mediumId, from, to);

        from = UtcTime.Normalize(from);
        to = UtcTime.Normalize(to);

        // Validate query parameters
        if (from.HasValue && to.HasValue && from.Value > to.Value)
        {
            throw new ValidationException(new Dictionary<string, string[]>
            {
                [nameof(from)] = ["'from' date must be before 'to' date."],
                [nameof(to)] = ["'to' date must be after 'from' date."]
            });
        }

        var query = _dbContext.Seances
            .Where(s => s.Status == SeanceStatus.Scheduled);

        if (mediumId.HasValue)
        {
            query = query.Where(s => s.MediumId == mediumId.Value);
        }

        if (from.HasValue)
        {
            query = query.Where(s => s.StartsAt >= from.Value);
        }

        if (to.HasValue)
        {
            query = query.Where(s => s.StartsAt <= to.Value);
        }

        var seances = await query
            .OrderBy(s => s.StartsAt)
            .ThenBy(s => s.Id)
            .Select(s => new SeanceDto(s.Id, s.MediumId, s.ClientName, s.StartsAt, s.EndsAt, s.Status, s.CreatedAt, s.CancelledAt))
            .ToListAsync(cancellationToken);

        _logger.LogInformation("Returning {Count} seances", seances.Count);
        return seances;
    }

    public async Task<SeanceDto> BookSeanceAsync(int mediumId, string clientName, DateTime startsAt, DateTime endsAt, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Booking seance for medium {MediumId} at {Time}", mediumId, DateTime.UtcNow);

        // Convert input times to UTC for validation
        var startsAtUtc = UtcTime.Normalize(startsAt);
        var endsAtUtc = UtcTime.Normalize(endsAt);

        // Pure input validation runs before the transaction
        ValidateBookingInput(startsAtUtc, endsAtUtc);

        // Microsoft.Data.Sqlite opens non-deferred transactions with BEGIN IMMEDIATE: the database write lock
        // is taken before the overlap check, so concurrent bookings (even from other processes sharing the
        // same DB file) are serialized and exactly one of two overlapping requests can succeed.
        await using var transaction = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var medium = await _dbContext.Mediums.AsNoTracking()
            .FirstOrDefaultAsync(m => m.Id == mediumId, cancellationToken);
        if (medium is null)
        {
            _logger.LogWarning("Medium {MediumId} not found", mediumId);
            throw new NotFoundException($"Medium {mediumId} not found.");
        }

        if (!medium.IsActive)
        {
            _logger.LogWarning("Medium {MediumId} is inactive", mediumId);
            throw new ConflictException("Cannot book a seance with an inactive medium.");
        }

        // Half-open intervals [StartsAt, EndsAt): back-to-back seances do not overlap.
        var hasOverlap = await _dbContext.Seances
            .Where(s => s.MediumId == mediumId && s.Status == SeanceStatus.Scheduled)
            .AnyAsync(s => s.StartsAt < endsAtUtc && s.EndsAt > startsAtUtc, cancellationToken);

        if (hasOverlap)
        {
            _logger.LogWarning("Seance overlaps with existing booking for medium {MediumId}", mediumId);
            throw new ConflictException("Seance overlaps with an existing booking for this medium.");
        }

        var seance = new Seance
        {
            MediumId = mediumId,
            ClientName = clientName.Trim(),
            StartsAt = startsAtUtc,
            EndsAt = endsAtUtc,
            Status = SeanceStatus.Scheduled,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        _dbContext.Seances.Add(seance);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);

        _logger.LogInformation("Created seance {SeanceId} for medium {MediumId}", seance.Id, mediumId);
        return new SeanceDto(seance.Id, seance.MediumId, seance.ClientName, seance.StartsAt, seance.EndsAt, seance.Status, seance.CreatedAt, seance.CancelledAt);
    }

    public async Task<SeanceDto> CancelSeanceAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Cancelling seance {SeanceId} at {Time}", id, DateTime.UtcNow);

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Use ExecuteUpdateAsync for atomic update with WHERE clause
        var rowsAffected = await _dbContext.Seances
            .Where(s => s.Id == id && s.Status == SeanceStatus.Scheduled)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(s => s.Status, SeanceStatus.Cancelled)
                .SetProperty(s => s.CancelledAt, now),
                cancellationToken);

        if (rowsAffected == 0)
        {
            // Check if seance exists but is already cancelled
            var seance = await _dbContext.Seances
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            if (seance == null)
            {
                _logger.LogWarning("Seance {SeanceId} not found", id);
                throw new NotFoundException($"Seance {id} not found.");
            }

            if (seance.Status == SeanceStatus.Cancelled)
            {
                _logger.LogWarning("Seance {SeanceId} already cancelled", id);
                throw new ConflictException("Seance has already been cancelled.");
            }

            // Should not reach here, but handle edge case
            _logger.LogWarning("Seance {SeanceId} could not be cancelled", id);
            throw new ConflictException("Seance could not be cancelled.");
        }

        // Read the updated seance with AsNoTracking
        var updatedSeance = await _dbContext.Seances
            .AsNoTracking()
            .FirstAsync(s => s.Id == id, cancellationToken);

        _logger.LogInformation("Cancelled seance {SeanceId}", id);
        return new SeanceDto(updatedSeance.Id, updatedSeance.MediumId, updatedSeance.ClientName,
            updatedSeance.StartsAt, updatedSeance.EndsAt, updatedSeance.Status, updatedSeance.CreatedAt, updatedSeance.CancelledAt);
    }

    private void ValidateBookingInput(DateTime startsAtUtc, DateTime endsAtUtc)
    {
        var errors = new Dictionary<string, List<string>>();
        void Add(string key, string message)
        {
            if (!errors.TryGetValue(key, out var list))
            {
                errors[key] = list = [];
            }

            list.Add(message);
        }

        var now = _timeProvider.GetUtcNow().UtcDateTime;

        // Duration validation
        var duration = (endsAtUtc - startsAtUtc).TotalMinutes;
        if (duration < CreateSeanceRequest.MinDurationMinutes)
        {
            Add(nameof(Seance.EndsAt), $"Duration must be at least {CreateSeanceRequest.MinDurationMinutes} minutes.");
        }
        else if (duration > CreateSeanceRequest.MaxDurationMinutes)
        {
            Add(nameof(Seance.EndsAt), $"Duration must be at most {CreateSeanceRequest.MaxDurationMinutes} minutes.");
        }
        else if (duration % CreateSeanceRequest.DurationIncrementMinutes != 0)
        {
            Add(nameof(Seance.EndsAt), $"Duration must be a multiple of {CreateSeanceRequest.DurationIncrementMinutes} minutes.");
        }

        // 15-minute boundary validation
        if (!UtcTime.IsOnBoundary(startsAtUtc, CreateSeanceRequest.DurationIncrementMinutes))
        {
            Add(nameof(Seance.StartsAt), "Start time must be on a 15-minute boundary (e.g., 14:00, 14:15, 14:30, 14:45).");
        }

        // Future start time validation
        if (startsAtUtc <= now)
        {
            Add(nameof(Seance.StartsAt), "Start time must be in the future.");
        }

        // Future end time validation
        if (endsAtUtc <= now)
        {
            Add(nameof(Seance.EndsAt), "End time must be in the future.");
        }

        if (errors.Count > 0)
        {
            throw new ValidationException(errors.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
        }
    }
}