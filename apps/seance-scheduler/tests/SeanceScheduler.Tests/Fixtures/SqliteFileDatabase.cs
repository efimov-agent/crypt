using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using SeanceScheduler.Web.Data;
using SeanceScheduler.Web.Features.Mediums;
using SeanceScheduler.Web.Features.Seances;
using Xunit;

namespace SeanceScheduler.Tests.Fixtures;

/// <summary>
/// A migrated, throw-away SQLite file per test class instance. Each <see cref="CreateContext"/> call opens
/// its own connection, so concurrency tests exercise real SQLite file locking rather than a shared connection.
/// </summary>
public sealed class SqliteFileDatabase : IAsyncLifetime
{
    /// <summary>Fixed "now" for service tests: 2030-06-01 12:00 UTC.</summary>
    public static readonly DateTimeOffset Now = new(2030, 6, 1, 12, 0, 0, TimeSpan.Zero);

    private readonly string _dbPath = Path.Combine(Path.GetTempPath(), $"seance_svc_{Guid.NewGuid():N}.db");

    public FakeTimeProvider Clock { get; } = new(Now);

    public ApplicationDbContext CreateContext() =>
        new(new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite($"Data Source={_dbPath};Pooling=False")
            .Options);

    public MediumsService CreateMediumsService(ApplicationDbContext db) =>
        new(db, NullLogger<MediumsService>.Instance);

    public SeancesService CreateSeancesService(ApplicationDbContext db) =>
        new(db, Clock, NullLogger<SeancesService>.Instance);

    public async Task InitializeAsync()
    {
        await using var db = CreateContext();
        await db.Database.MigrateAsync();
    }

    public Task DisposeAsync()
    {
        if (File.Exists(_dbPath))
        {
            File.Delete(_dbPath);
        }

        return Task.CompletedTask;
    }
}
