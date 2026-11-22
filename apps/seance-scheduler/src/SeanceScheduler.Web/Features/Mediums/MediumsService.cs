using SeanceScheduler.Web.Data;
using SeanceScheduler.Web.Data.Models;
using SeanceScheduler.Web.Features.Common;
using Microsoft.EntityFrameworkCore;

namespace SeanceScheduler.Web.Features.Mediums;

public class MediumsService
{
    private readonly ApplicationDbContext _dbContext;
    private readonly ILogger<MediumsService> _logger;

    public MediumsService(ApplicationDbContext dbContext, ILogger<MediumsService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }

    public async Task<IReadOnlyList<MediumDto>> GetMediumsAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching all mediums at {Time}", DateTime.UtcNow);

        return await _dbContext.Mediums
            .OrderBy(m => m.Name)
            .Select(m => new MediumDto(m.Id, m.Name, m.Specialty, m.IsActive, m.CreatedAt))
            .ToListAsync(cancellationToken);
    }

    public async Task<MediumDto?> GetMediumByIdAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Fetching medium {MediumId} at {Time}", id, DateTime.UtcNow);

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
        _logger.LogInformation("Creating medium {Name} at {Time}", name, DateTime.UtcNow);

        var trimmedName = name.Trim();
        var trimmedSpecialty = string.IsNullOrWhiteSpace(specialty) ? null : specialty.Trim();

        // Check for duplicate name (case-insensitive)
        var existingMedium = await _dbContext.Mediums
            .FirstOrDefaultAsync(m => m.Name.ToLower() == trimmedName.ToLower(), cancellationToken);

        if (existingMedium != null)
        {
            _logger.LogWarning("Medium with name {Name} already exists", trimmedName);
            throw new ConflictException($"A medium with the name '{trimmedName}' already exists.");
        }

        var medium = new Medium
        {
            Name = trimmedName,
            Specialty = trimmedSpecialty,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Mediums.Add(medium);
        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Created medium {MediumId} - {MediumName}", medium.Id, medium.Name);
        return new MediumDto(medium.Id, medium.Name, medium.Specialty, medium.IsActive, medium.CreatedAt);
    }

    public async Task<MediumDto> DeactivateMediumAsync(int id, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Deactivating medium {MediumId} at {Time}", id, DateTime.UtcNow);

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