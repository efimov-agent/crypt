using Microsoft.EntityFrameworkCore;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Data.Models;
using SeanceScheduler.Web.Features.Common;
using SeanceScheduler.Web.Features.Seances;
using Xunit;

namespace SeanceScheduler.Tests.Features;

public class SeancesServiceTests : IAsyncLifetime
{
    private readonly SqliteFileDatabase _database = new();

    // A future day (relative to the fake clock) used as the base for bookings.
    private static readonly DateTime Day = new(2030, 6, 2, 0, 0, 0, DateTimeKind.Utc);

    private int _mediumId;

    public async Task InitializeAsync()
    {
        await _database.InitializeAsync();
        _mediumId = await CreateMediumAsync("Leota");
    }

    public Task DisposeAsync() => _database.DisposeAsync();

    private static DateTime At(int hour, int minute = 0) => Day.AddHours(hour).AddMinutes(minute);

    private async Task<int> CreateMediumAsync(string name, bool active = true)
    {
        await using var db = _database.CreateContext();
        var service = _database.CreateMediumsService(db);
        var medium = await service.CreateMediumAsync(name, null, CancellationToken.None);
        if (!active)
        {
            await service.DeactivateMediumAsync(medium.Id, CancellationToken.None);
        }

        return medium.Id;
    }

    private async Task<SeanceDto> BookAsync(DateTime startsAt, DateTime endsAt, int? mediumId = null, string client = "Ichabod")
    {
        await using var db = _database.CreateContext();
        return await _database.CreateSeancesService(db)
            .BookSeanceAsync(mediumId ?? _mediumId, client, startsAt, endsAt, CancellationToken.None);
    }

    private async Task<SeanceDto> CancelAsync(int id)
    {
        await using var db = _database.CreateContext();
        return await _database.CreateSeancesService(db).CancelSeanceAsync(id, CancellationToken.None);
    }

    private async Task<IReadOnlyList<SeanceDto>> ListAsync(int? mediumId = null, DateTime? from = null, DateTime? to = null)
    {
        await using var db = _database.CreateContext();
        return await _database.CreateSeancesService(db).GetSeancesAsync(mediumId, from, to, CancellationToken.None);
    }

    private async Task<int> CountScheduledAsync()
    {
        await using var db = _database.CreateContext();
        return await db.Seances.CountAsync(s => s.Status == SeanceStatus.Scheduled);
    }

    // ---- Part A: validation ----

    [Theory]
    [InlineData(29)]
    [InlineData(241)]
    [InlineData(40)] // not a multiple of 15
    public async Task Book_InvalidDuration_ThrowsValidationOnEndsAt(int minutes)
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => BookAsync(At(14), At(14).AddMinutes(minutes)));

        Assert.Equal(["EndsAt"], ex.Errors.Keys);
        Assert.Equal(0, await CountScheduledAsync());
    }

    [Theory]
    [InlineData(30)]
    [InlineData(45)]
    [InlineData(240)]
    public async Task Book_DurationWithinLimits_Succeeds(int minutes)
    {
        var seance = await BookAsync(At(14), At(14).AddMinutes(minutes));

        Assert.Equal(TimeSpan.FromMinutes(minutes), seance.EndsAt - seance.StartsAt);
        Assert.Equal(SeanceStatus.Scheduled, seance.Status);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(15)]
    [InlineData(30)]
    [InlineData(45)]
    public async Task Book_StartOnQuarterHour_Succeeds(int minute)
    {
        var seance = await BookAsync(At(14, minute), At(14, minute).AddMinutes(30));

        Assert.Equal(At(14, minute), seance.StartsAt);
    }

    [Theory]
    [InlineData(10, 0)]
    [InlineData(0, 30)] // 14:00:30 — seconds are not on the grid
    public async Task Book_StartOffGrid_ThrowsValidationOnStartsAt(int minute, int second)
    {
        var start = At(14, minute).AddSeconds(second);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => BookAsync(start, start.AddMinutes(30)));

        Assert.Contains("StartsAt", ex.Errors.Keys);
    }

    [Fact]
    public async Task Book_StartInPast_ThrowsValidation()
    {
        var past = SqliteFileDatabase.Now.UtcDateTime.AddHours(-2); // 10:00, on the grid

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => BookAsync(past, past.AddMinutes(60)));

        Assert.Contains("StartsAt", ex.Errors.Keys);
    }

    [Fact]
    public async Task Book_PastAndOffGrid_ReportsBothStartErrors()
    {
        var start = SqliteFileDatabase.Now.UtcDateTime.AddHours(-2).AddMinutes(10);

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => BookAsync(start, start.AddMinutes(30)));

        Assert.Equal(2, ex.Errors["StartsAt"].Length);
    }

    [Fact]
    public async Task Book_StartExactlyNow_ThrowsValidation()
    {
        var now = SqliteFileDatabase.Now.UtcDateTime;

        await Assert.ThrowsAsync<RequestValidationException>(() => BookAsync(now, now.AddMinutes(30)));
    }

    [Fact]
    public async Task Book_NextSlotAfterNow_Succeeds_AndStoresUtcAndCreatedAt()
    {
        var start = SqliteFileDatabase.Now.UtcDateTime.AddMinutes(15);

        var seance = await BookAsync(start, start.AddMinutes(30), client: "  Ichabod  ");

        Assert.Equal("Ichabod", seance.ClientName);
        Assert.Equal(SqliteFileDatabase.Now.UtcDateTime, seance.CreatedAt);
        Assert.Null(seance.CancelledAt);
        var stored = Assert.Single(await ListAsync());
        Assert.Equal(DateTimeKind.Utc, stored.StartsAt.Kind);
        Assert.Equal(start, stored.StartsAt);
    }

    [Fact]
    public async Task Book_UnspecifiedKind_IsTreatedAsUtc()
    {
        var start = DateTime.SpecifyKind(At(14), DateTimeKind.Unspecified);

        var seance = await BookAsync(start, start.AddMinutes(30));

        Assert.Equal(At(14), seance.StartsAt);
        Assert.Equal(DateTimeKind.Utc, seance.StartsAt.Kind);
    }

    // ---- Part B: business rules ----

    [Fact]
    public async Task Book_UnknownMedium_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => BookAsync(At(14), At(15), mediumId: 4242));
    }

    [Fact]
    public async Task Book_InactiveMedium_ThrowsConflict()
    {
        var inactive = await CreateMediumAsync("Retired", active: false);

        await Assert.ThrowsAsync<ConflictException>(() => BookAsync(At(14), At(15), mediumId: inactive));
        Assert.Equal(0, await CountScheduledAsync());
    }

    [Fact]
    public async Task Book_BackToBack_BothSides_Succeeds()
    {
        await BookAsync(At(14, 30), At(15, 30));

        await BookAsync(At(13, 30), At(14, 30)); // ends exactly when existing starts
        await BookAsync(At(15, 30), At(16));     // starts exactly when existing ends

        Assert.Equal(3, await CountScheduledAsync());
    }

    [Theory]
    [InlineData(13, 30, 14, 30)] // overlaps the start of 14:00–15:00
    [InlineData(14, 30, 15, 30)] // overlaps the end
    [InlineData(13, 0, 16, 0)]   // contains the existing seance
    [InlineData(14, 15, 14, 45)] // contained in the existing seance
    [InlineData(14, 0, 15, 0)]   // identical slot
    public async Task Book_Overlap_ThrowsConflict(int h1, int m1, int h2, int m2)
    {
        await BookAsync(At(14), At(15));

        await Assert.ThrowsAsync<ConflictException>(() => BookAsync(At(h1, m1), At(h2, m2)));
        Assert.Equal(1, await CountScheduledAsync());
    }

    [Fact]
    public async Task Book_SameSlotOtherMedium_Succeeds()
    {
        var other = await CreateMediumAsync("Morgana");
        await BookAsync(At(14), At(15));

        await BookAsync(At(14), At(15), mediumId: other);

        Assert.Equal(2, await CountScheduledAsync());
    }

    [Fact]
    public async Task Book_OverCancelledSeance_Succeeds()
    {
        var first = await BookAsync(At(14), At(15));
        await CancelAsync(first.Id);

        var second = await BookAsync(At(14), At(15));

        Assert.NotEqual(first.Id, second.Id);
        Assert.Equal([second.Id], (await ListAsync()).Select(s => s.Id));
    }

    // ---- Part C: cancel, queries, concurrency ----

    [Fact]
    public async Task Cancel_Scheduled_SetsCancelledStatusAndTimestamp()
    {
        var seance = await BookAsync(At(14), At(15));
        _database.Clock.Advance(TimeSpan.FromMinutes(5));

        var cancelled = await CancelAsync(seance.Id);

        Assert.Equal(SeanceStatus.Cancelled, cancelled.Status);
        Assert.Equal(SqliteFileDatabase.Now.UtcDateTime.AddMinutes(5), cancelled.CancelledAt);
        Assert.Empty(await ListAsync());
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_ThrowsConflict_AndKeepsFirstTimestamp()
    {
        var seance = await BookAsync(At(14), At(15));
        var first = await CancelAsync(seance.Id);
        _database.Clock.Advance(TimeSpan.FromMinutes(10));

        await Assert.ThrowsAsync<ConflictException>(() => CancelAsync(seance.Id));

        await using var db = _database.CreateContext();
        Assert.Equal(first.CancelledAt, (await db.Seances.SingleAsync(s => s.Id == seance.Id)).CancelledAt);
    }

    [Fact]
    public async Task Cancel_Unknown_ThrowsNotFound()
    {
        await Assert.ThrowsAsync<NotFoundException>(() => CancelAsync(4242));
    }

    [Fact]
    public async Task GetSeances_NoFilters_ReturnsOnlyScheduled_OrderedByStartThenId()
    {
        var other = await CreateMediumAsync("Morgana");
        var late = await BookAsync(At(16), At(17));
        var earlyB = await BookAsync(At(14), At(15), mediumId: other);
        var earlyA = await BookAsync(At(14), At(15));
        var cancelled = await BookAsync(At(18), At(19));
        await CancelAsync(cancelled.Id);

        var ids = (await ListAsync()).Select(s => s.Id).ToList();

        Assert.Equal(new[] { earlyB.Id, earlyA.Id }.Order().Append(late.Id), ids);
    }

    [Fact]
    public async Task GetSeances_FilterByMedium()
    {
        var other = await CreateMediumAsync("Morgana");
        var mine = await BookAsync(At(14), At(15));
        await BookAsync(At(14), At(15), mediumId: other);

        var result = await ListAsync(mediumId: _mediumId);

        Assert.Equal([mine.Id], result.Select(s => s.Id));
    }

    [Fact]
    public async Task GetSeances_FilterByRange_IsInclusiveOnStartsAt()
    {
        var s13 = await BookAsync(At(13), At(14));
        var s14 = await BookAsync(At(14), At(15));
        var s15 = await BookAsync(At(15), At(16));
        await BookAsync(At(16), At(17));

        var result = await ListAsync(from: At(14), to: At(15));
        Assert.Equal([s14.Id, s15.Id], result.Select(s => s.Id));

        Assert.Equal([s13.Id], (await ListAsync(to: At(13, 59))).Select(s => s.Id));
    }

    [Fact]
    public async Task GetSeances_FromAfterTo_ThrowsValidation()
    {
        await Assert.ThrowsAsync<RequestValidationException>(() => ListAsync(from: At(15), to: At(14)));
    }

    [Fact]
    public async Task Book_ConcurrentOverlapping_ExactlyOneSucceeds()
    {
        const int attempts = 20;
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, attempts).Select(i => Task.Run(async () =>
        {
            await gate.Task;
            // Each attempt uses its own DbContext/connection against the same database file.
            // Slots differ but all overlap 14:00–15:00, so at most one can win.
            var start = At(13, 15 * (i % 4) + 15);
            try
            {
                await BookAsync(start, start.AddMinutes(60), client: $"Client {i}");
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
        Assert.Equal(attempts - 1, results.Count(r => !r));
        Assert.Equal(1, await CountScheduledAsync());
    }

    [Fact]
    public async Task Cancel_Concurrent_ExactlyOneSucceeds()
    {
        var seance = await BookAsync(At(14), At(15));
        var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

        var tasks = Enumerable.Range(0, 2).Select(_ => Task.Run(async () =>
        {
            await gate.Task;
            try
            {
                await CancelAsync(seance.Id);
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
        Assert.Equal(1, results.Count(r => !r));
    }

    [Fact]
    public async Task Book_RacingDeactivation_EitherBooksBeforeOrConflictsAfter()
    {
        // Booking reads IsActive inside BEGIN IMMEDIATE, deactivation is a write: the two are serialized,
        // so a booking either commits before the deactivation or sees the medium inactive (409) — never a lock error.
        const int rounds = 10;
        for (var round = 0; round < rounds; round++)
        {
            var mediumId = await CreateMediumAsync($"Racer {round}");
            var gate = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);

            var booking = Task.Run(async () =>
            {
                await gate.Task;
                try
                {
                    await BookAsync(At(14), At(15), mediumId);
                    return true;
                }
                catch (ConflictException)
                {
                    return false;
                }
            });
            var deactivation = Task.Run(async () =>
            {
                await gate.Task;
                await using var db = _database.CreateContext();
                await _database.CreateMediumsService(db).DeactivateMediumAsync(mediumId, CancellationToken.None);
            });

            gate.SetResult();
            var booked = await booking;
            await deactivation;

            await using var check = _database.CreateContext();
            Assert.False((await check.Mediums.SingleAsync(m => m.Id == mediumId)).IsActive);
            Assert.Equal(booked ? 1 : 0, await check.Seances.CountAsync(s => s.MediumId == mediumId));

            // Once deactivation has committed, booking is always refused.
            var ex = await Assert.ThrowsAsync<ConflictException>(() => BookAsync(At(16), At(17), mediumId));
            Assert.Contains("inactive", ex.Message);
        }
    }
}
