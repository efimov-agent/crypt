using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Features.Mediums;
using Xunit;

namespace SeanceScheduler.Tests.Features;

/// <summary>App with a 1-second SQLite busy timeout so a held write lock surfaces quickly.</summary>
public sealed class ShortBusyTimeoutFixture : TestDatabaseFixture
{
    protected override string ConnectionOptions => ";Default Timeout=1";
}

[Collection("Integration tests")]
public class DatabaseBusyTests : IClassFixture<ShortBusyTimeoutFixture>
{
    private readonly ShortBusyTimeoutFixture _fixture;
    private readonly HttpClient _client;

    public DatabaseBusyTests(ShortBusyTimeoutFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    [Fact]
    public async Task Booking_WhileAnotherWriterHoldsTheLock_Returns503ProblemJson()
    {
        var created = await _client.PostAsJsonAsync("/api/mediums", new { name = $"Medium {Guid.NewGuid():N}" });
        var medium = (await created.Content.ReadFromJsonAsync<MediumDto>())!;
        var start = DateTime.UtcNow.Date.AddDays(30).AddHours(14);

        await using (var locker = new SqliteConnection($"Data Source={_fixture.DbPath};Pooling=False"))
        {
            await locker.OpenAsync();
            await using var hold = locker.CreateCommand();
            hold.CommandText = "BEGIN IMMEDIATE;";
            await hold.ExecuteNonQueryAsync();

            var response = await _client.PostAsJsonAsync("/api/seances",
                new { mediumId = medium.Id, clientName = "Ichabod", startsAt = start, endsAt = start.AddHours(1) });

            Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
            Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetails>();
            Assert.Equal(503, problem!.Status);

            hold.CommandText = "ROLLBACK;";
            await hold.ExecuteNonQueryAsync();
        }

        // Once the lock is released the same booking succeeds.
        var retry = await _client.PostAsJsonAsync("/api/seances",
            new { mediumId = medium.Id, clientName = "Ichabod", startsAt = start, endsAt = start.AddHours(1) });
        Assert.Equal(HttpStatusCode.Created, retry.StatusCode);
    }
}
