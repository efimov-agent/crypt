using Microsoft.Data.Sqlite;
using SeanceScheduler.Web.Data;
using SeanceScheduler.Web.Data.Models;
using SeanceScheduler.Web.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace SeanceScheduler.Web.Features.Mediums;

public class MediumsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<MediumsService> _logger;

    public MediumsService(ApplicationDbContext dbContext, TimeProvider timeProvider, ILogger<MediumsService> logger)
    {
        _dbContext = dbContext;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediumDto>> GetMediumsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all mediums");

        return await _dbContext.Mediums
            .OrderBy(m => m.Name)
            .Select(m => new MediumDto(m.Id, m.Name, m.Specialty, m.IsActive, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MediumDto?> GetMediumByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching medium {MediumId}", id);

        var medium = await _dbContext.Mediums.FindAsync([id], cancellationToken);
        if (medium == null)
        {
            _logger.LogWarning("Medium {MediumId} not found", id);
            return null;
        }

        return new MediumDto(medium.Id, medium.Name, medium.Specialty, medium.IsActive, medium.CreatedAt);
    }

    public async Task<MediumDto> CreateMediumAsync(string name, string? specialty, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Creating medium {Name}", name);

        var trimmedName = name.Trim();
        var trimmedSpecialty = string.IsNullOrWhiteSpace(specialty) ? null : specialty.Trim();

        var normalizedName = Medium.Normalize(trimmedName);

        // Duplicate check is case-insensitive for any script (the unique index on NormalizedName backs it up)
        var existingMedium = await _dbContext.Mediums
            .FirstOrDefaultAsync(m => m.NormalizedName == normalizedName, cancellationToken);

        if (existingMedium != null)
        {
            _logger.LogWarning("Medium with name {Name} already exists", trimmedName);
            throw new ConflictException($"A medium with the name '{trimmedName}' already exists.");
        }

        var medium = new Medium
        {
            Name = trimmedName,
            NormalizedName = normalizedName,
            Specialty = trimmedSpecialty,
            IsActive = true,
            CreatedAt = _timeProvider.GetUtcNow().UtcDateTime
        };

        _dbContext.Mediums.Add(medium);
        try
        {
            await _dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (ex.InnerException is SqliteException { SqliteExtendedErrorCode: 2067 })
        {
            // SQLITE_CONSTRAINT_UNIQUE: a concurrent request inserted the same name after our check.
            _logger.LogWarning("Medium with name {Name} already exists (unique index)", trimmedName);
            throw new ConflictException($"A medium with the name '{trimmedName}' already exists.");
        }

        _logger.LogInformation("Created medium {MediumId} - {MediumName}", medium.Id, medium.Name);
        return new MediumDto(medium.Id, medium.Name, medium.Specialty, medium.IsActive, medium.CreatedAt);
    }

    public async Task<MediumDto> DeactivateMediumAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating medium {MediumId}", id);

        var medium = await _dbContext.Mediums.FindAsync([id], cancellationToken);
        if (medium == null)
        {
            _logger.LogWarning("Medium {MediumId} not found for deactivation", id);
            throw new NotFoundException($"Medium {id} not found.");
        }

        medium.IsActive = false;
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Deactivated medium {MediumId}", id);
        return new MediumDto(medium.Id, medium.Name, medium.Specialty, medium.IsActive, medium.CreatedAt);
    }
}