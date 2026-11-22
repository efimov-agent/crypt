using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Data.Models;
using SeanceScheduler.Web.Features.Mediums;
using SeanceScheduler.Web.Features.Seances;
using Xunit;

namespace SeanceScheduler.Tests.Features;

[Collection("Integration tests")]
public class SeancesEndpointTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    // The app uses the real clock here, so bookings go well into the future.
    private static readonly DateTime Day = DateTime.UtcNow.Date.AddDays(30);

    public SeancesEndpointTests(TestDatabaseFixture fixture)
    {
        _client = fixture.CreateClient();
    }

    private static DateTime At(int hour, int minute = 0) => Day.AddHours(hour).AddMinutes(minute);

    private async Task<int> CreateMediumAsync(bool active = true)
    {
        var response = await _client.PostAsJsonAsync("/api/mediums", new { name = $"Medium {Guid.NewGuid():N}" });
        var medium = (await response.Content.ReadFromJsonAsync<MediumDto>())!;
        if (!active)
        {
            (await _client.PostAsync($"/api/mediums/{medium.Id}/deactivate", null)).EnsureSuccessStatusCode();
        }

        return medium.Id;
    }

    private Task<HttpResponseMessage> PostSeanceAsync(int mediumId, DateTime startsAt, DateTime endsAt, string clientName = "Ichabod") =>
        _client.PostAsJsonAsync("/api/seances", new { mediumId, clientName, startsAt, endsAt });

    private async Task<SeanceDto> BookAsync(int mediumId, DateTime startsAt, DateTime endsAt)
    {
        var response = await PostSeanceAsync(mediumId, startsAt, endsAt);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SeanceDto>())!;
    }

    private async Task<List<SeanceDto>> ListAsync(string query) =>
        (await _client.GetFromJsonAsync<List<SeanceDto>>($"/api/seances{query}"))!;

    private static string Iso(DateTime value) => Uri.EscapeDataString(value.ToString("O"));

    // ---- Happy paths ----

    [Fact]
    public async Task Create_Valid_Returns201WithLocationAndDto()
    {
        var mediumId = await CreateMediumAsync();

        var response = await PostSeanceAsync(mediumId, At(14), At(15), "  Ichabod Crane ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var seance = await response.Content.ReadFromJsonAsync<SeanceDto>();
        Assert.NotNull(seance);
        Assert.Equal($"/api/seances/{seance.Id}", response.Headers.Location?.OriginalString);
        Assert.Equal(mediumId, seance.MediumId);
        Assert.Equal("Ichabod Crane", seance.ClientName);
        Assert.Equal(At(14), seance.StartsAt);
        Assert.Equal(At(15), seance.EndsAt);
        Assert.Equal(SeanceStatus.Scheduled, seance.Status);
        Assert.Null(seance.CancelledAt);
    }

    [Fact]
    public async Task Create_WithTimeZoneOffset_IsStoredAsUtc()
    {
        var mediumId = await CreateMediumAsync();
        var payload = $$"""{"mediumId":{{mediumId}},"clientName":"Ichabod","startsAt":"{{Day:yyyy-MM-dd}}T16:00:00+02:00","endsAt":"{{Day:yyyy-MM-dd}}T17:00:00+02:00"}""";

        var response = await _client.PostAsync("/api/seances", new StringContent(payload, System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var seance = (await response.Content.ReadFromJsonAsync<SeanceDto>())!;
        Assert.Equal(At(14), seance.StartsAt.ToUniversalTime());
    }

    [Fact]
    public async Task List_ReturnsScheduledOrderedByStart_FilteredByMedium()
    {
        var mediumId = await CreateMediumAsync();
        var late = await BookAsync(mediumId, At(16), At(17));
        var early = await BookAsync(mediumId, At(10), At(11));
        var cancelled = await BookAsync(mediumId, At(12), At(13));
        (await _client.PostAsync($"/api/seances/{cancelled.Id}/cancel", null)).EnsureSuccessStatusCode();
        await BookAsync(await CreateMediumAsync(), At(10), At(11)); // other medium

        var result = await ListAsync($"?mediumId={mediumId}");

        Assert.Equal([early.Id, late.Id], result.Select(s => s.Id));
    }

    [Fact]
    public async Task List_FromTo_FiltersByStart()
    {
        var mediumId = await CreateMediumAsync();
        await BookAsync(mediumId, At(9), At(10));
        var inRange = await BookAsync(mediumId, At(11), At(12));
        await BookAsync(mediumId, At(13), At(14));

        var result = await ListAsync($"?mediumId={mediumId}&from={Iso(At(10, 30))}&to={Iso(At(12, 30))}");

        Assert.Equal([inRange.Id], result.Select(s => s.Id));
    }

    [Fact]
    public async Task Cancel_Scheduled_Returns200WithCancelledStatus()
    {
        var mediumId = await CreateMediumAsync();
        var seance = await BookAsync(mediumId, At(14), At(15));

        var response = await _client.PostAsync($"/api/seances/{seance.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cancelled = (await response.Content.ReadFromJsonAsync<SeanceDto>())!;
        Assert.Equal(SeanceStatus.Cancelled, cancelled.Status);
        Assert.NotNull(cancelled.CancelledAt);
        Assert.Empty(await ListAsync($"?mediumId={mediumId}"));
    }

    // ---- Error cases ----

    [Theory]
    [InlineData(0, 29)]   // too short
    [InlineData(0, 241)]  // too long
    [InlineData(0, 50)]   // not a multiple of 15
    [InlineData(10, 60)]  // start off the 15-minute grid
    public async Task Create_InvalidSlot_Returns400(int startMinute, int durationMinutes)
    {
        var mediumId = await CreateMediumAsync();
        var start = At(14, startMinute);

        var response = await PostSeanceAsync(mediumId, start, start.AddMinutes(durationMinutes));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
        Assert.Empty(await ListAsync($"?mediumId={mediumId}"));
    }

    [Fact]
    public async Task Create_PastStart_Returns400WithStartsAtError()
    {
        var mediumId = await CreateMediumAsync();
        var start = DateTime.UtcNow.Date.AddDays(-1).AddHours(14);

        var response = await PostSeanceAsync(mediumId, start, start.AddHours(1));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("StartsAt", problem!.Errors.Keys);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  ")]
    public async Task Create_MissingClientName_Returns400(string clientName)
    {
        var mediumId = await CreateMediumAsync();

        var response = await PostSeanceAsync(mediumId, At(14), At(15), clientName);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>();
        Assert.Contains("ClientName", problem!.Errors.Keys);
    }

    [Fact]
    public async Task Create_UnknownMedium_Returns404()
    {
        var response = await PostSeanceAsync(999999, At(14), At(15));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Create_InactiveMedium_Returns409()
    {
        var mediumId = await CreateMediumAsync(active: false);

        var response = await PostSeanceAsync(mediumId, At(14), At(15));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task Create_Overlapping_Returns409ProblemDetails_BackToBackAllowed()
    {
        var mediumId = await CreateMediumAsync();
        await BookAsync(mediumId, At(14), At(15));

        var overlap = await PostSeanceAsync(mediumId, At(14, 30), At(15, 30));
        var backToBack = await PostSeanceAsync(mediumId, At(15), At(16));

        Assert.Equal(HttpStatusCode.Conflict, overlap.StatusCode);
        Assert.Equal("application/problem+json", overlap.Content.Headers.ContentType?.MediaType);
        Assert.Equal(409, (await overlap.Content.ReadFromJsonAsync<ProblemDetails>())?.Status);
        Assert.Equal(HttpStatusCode.Created, backToBack.StatusCode);
    }

    [Fact]
    public async Task Create_ConcurrentOverlapping_ExactlyOne201()
    {
        var mediumId = await CreateMediumAsync();

        var responses = await Task.WhenAll(Enumerable.Range(0, 20)
            .Select(_ => PostSeanceAsync(mediumId, At(20), At(21))));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.Equal(19, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
        Assert.Single(await ListAsync($"?mediumId={mediumId}"));
    }

    [Fact]
    public async Task List_FromAfterTo_Returns400()
    {
        var response = await _client.GetAsync($"/api/seances?from={Iso(At(15))}&to={Iso(At(14))}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(await response.Content.ReadFromJsonAsync<ValidationProblemDetails>());
    }

    [Theory]
    [InlineData("?mediumId=abc")]
    [InlineData("?from=not-a-date")]
    public async Task List_UnparsableQuery_Returns400ProblemJson(string query)
    {
        var response = await _client.GetAsync($"/api/seances{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Create_NullMediumId_Returns400ProblemJson()
    {
        var response = await _client.PostAsync("/api/seances",
            new StringContent("{\"mediumId\":null}", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task UnknownApiRoute_Returns404ProblemJson()
    {
        var response = await _client.GetAsync("/api/does-not-exist");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task Cancel_Unknown_Returns404()
    {
        var response = await _client.PostAsync("/api/seances/999999/cancel", null);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Cancel_AlreadyCancelled_Returns409()
    {
        var mediumId = await CreateMediumAsync();
        var seance = await BookAsync(mediumId, At(14), At(15));
        (await _client.PostAsync($"/api/seances/{seance.Id}/cancel", null)).EnsureSuccessStatusCode();

        var response = await _client.PostAsync($"/api/seances/{seance.Id}/cancel", null);

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }
}
