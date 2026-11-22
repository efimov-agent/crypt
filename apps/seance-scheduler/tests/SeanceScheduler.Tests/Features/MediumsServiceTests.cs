using Microsoft.EntityFrameworkCore;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Features.Common;
using SeanceScheduler.Web.Features.Mediums;
using Xunit;

namespace SeanceScheduler.Tests.Features;

public class MediumsServiceTests : IAsyncLifetime
{
    private readonly SqliteFileDatabase _database = new();

    public Task InitializeAsync() => _database.InitializeAsync();

    public Task DisposeAsync() => _database.DisposeAsync();

    private async Task<MediumDto> CreateAsync(string name, string? specialty = null)
    {
        await using var db = _database.CreateContext();
        return await _database.CreateMediumsService(db).CreateMediumAsync(name, specialty, CancellationToken.None);
    }

    [Fact]
    public async Task GetMediums_ReturnsAllOrderedByName()
    {
        await CreateAsync("Zelda");
        await CreateAsync("Agatha");
        var deactivated = await CreateAsync("Morgana");
        await using (var db = _database.CreateContext())
        {
            await _database.CreateMediumsService(db).DeactivateMediumAsync(deactivated.Id, CancellationToken.None);
        }

        await using var readDb = _database.CreateContext();
        var mediums = await _database.CreateMediumsService(readDb).GetMediumsAsync(CancellationToken.None);

        Assert.Equal(["Agatha", "Morgana", "Zelda"], mediums.Select(m => m.Name));
        Assert.False(mediums.Single(m => m.Name == "Morgana").IsActive);
    }

    [Fact]
    public async Task CreateMedium_ValidData_PersistsActiveMedium()
    {
        var created = await CreateAsync("Madame Leota", "Crystal ball");

        Assert.True(created.Id > 0);
        Assert.True(created.IsActive);
        await using var db = _database.CreateContext();
        var stored = await db.Mediums.SingleAsync(m => m.Id == created.Id);
        Assert.Equal("Madame Leota", stored.Name);
        Assert.Equal("Crystal ball", stored.Specialty);
        Assert.True(stored.IsActive);
    }

    [Fact]
    public async Task CreateMedium_TrimsNameAndSpecialty()
    {
        var created = await CreateAsync("  Leota  ", "  Tarot  ");

        Assert.Equal("Leota", created.Name);
        Assert.Equal("Tarot", created.Specialty);
    }

    [Fact]
    public async Task CreateMedium_WhitespaceSpecialty_StoredAsNull()
    {
        var created = await CreateAsync("Leota", "   ");

        Assert.Null(created.Specialty);
    }

    [Theory]
    [InlineData("Leota")]
    [InlineData("LEOTA")]
    [InlineData("  leota ")]
    public async Task CreateMedium_DuplicateName_AnyCase_ThrowsConflict(string duplicate)
    {
        await CreateAsync("Leota");

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync(duplicate));

        await using var db = _database.CreateContext();
        Assert.Equal(1, await db.Mediums.CountAsync());
    }

    [Fact]
    public async Task CreateMedium_DuplicateCyrillicName_DifferentCase_ThrowsConflict()
    {
        await CreateAsync("Ведьма");

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync("ВЕДЬМА"));
    }

    [Fact]
    public async Task CreateMedium_ComposedAndDecomposedAccents_AreDuplicates()
    {
        await CreateAsync("Ren\u00e9e");

        await Assert.ThrowsAsync<ConflictException>(() => CreateAsync("RENE\u0301E"));
    }

    [Fact]
    public async Task CreateMedium_UsesClockForCreatedAt()
    {
        var created = await CreateAsync("Leota");

        Assert.Equal(SqliteFileDatabase.Now.UtcDateTime, created.CreatedAt);
    }

    [Fact]
    public async Task CreateMedium_ConcurrentSameName_ExactlyOneSucceeds()
    {
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var tasks = Enumerable.Range(0, 10).Select(i => Task.Run(async () =>
        {
            await gate.Task;
            try
            {
                await CreateAsync(i % 2 == 0 ? "Leota" : "LEOTA");
                return true;
            }
            catch (ConflictException)
            {
                return false;
            }
        })).ToArray();

        gate.SetResult();
        var results = await Task.WhenAll(tasks);

        Assert.Equal(1, results.Count(r => r));
        await using var db = _database.CreateContext();
        Assert.Equal(1, await db.Mediums.CountAsync());
    }

    [Fact]
    public async Task DeactivateMedium_Active_SetsIsActiveFalse()
    {
        var created = await CreateAsync("Leota");

        await using (var db = _database.CreateContext())
        {
            var result = await _database.CreateMediumsService(db).DeactivateMediumAsync(created.Id, CancellationToken.None);
            Assert.False(result.IsActive);
        }

        await using var readDb = _database.CreateContext();
        Assert.False((await readDb.Mediums.SingleAsync(m => m.Id == created.Id)).IsActive);
    }

    [Fact]
    public async Task DeactivateMedium_Unknown_ThrowsNotFound()
    {
        await using var db = _database.CreateContext();

        await Assert.ThrowsAsync<NotFoundException>(
            () => _database.CreateMediumsService(db).DeactivateMediumAsync(4242, CancellationToken.None));
    }
}
