using System.Net;
using System.Net.Http.Json;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Features.Mediums;
using SeanceScheduler.Web.Features.Seances;
using Xunit;

namespace SeanceScheduler.Tests.Features;

[Collection("Integration tests")]
public class SchedulePageTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    // The app uses the real clock, so bookings go well into the future (other classes use other days).
    private static readonly DateTime Day = DateTime.UtcNow.Date.AddDays(50);

    public SchedulePageTests(TestDatabaseFixture fixture)
    {
        _client = PageForms.CreateNoRedirectClient(fixture);
    }

    private static DateTime At(int dayOffset, int hour) => Day.AddDays(dayOffset).AddHours(hour);

    private async Task<int> CreateMediumAsync()
    {
        var response = await _client.PostAsJsonAsync("/api/mediums", new { name = $"Medium {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<MediumDto>())!.Id;
    }

    private async Task<SeanceDto> BookAsync(int mediumId, DateTime startsAt, string? clientName = null)
    {
        var response = await _client.PostAsJsonAsync("/api/seances",
            new { mediumId, clientName = clientName ?? $"Seeker {Guid.NewGuid():N}", startsAt, endsAt = startsAt.AddHours(1) });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<SeanceDto>())!;
    }

    // The list endpoint returns scheduled seances only.
    private async Task<List<SeanceDto>> ScheduledOfAsync(int mediumId) =>
        (await _client.GetFromJsonAsync<List<SeanceDto>>($"/api/seances?mediumId={mediumId}"))!;

    private Task<HttpResponseMessage> PostCancelAsync(string url, string cancelId) =>
        PageForms.PostAsync(_client, url, "CancelSeance", [new("CancelId", cancelId)]);

    private static string DayHeadingId(DateTime day) => $"day-{day:yyyyMMdd}";

    [Fact]
    public async Task Get_GroupsSeancesByDay()
    {
        var mediumId = await CreateMediumAsync();
        var first = await BookAsync(mediumId, At(0, 10));
        var second = await BookAsync(mediumId, At(0, 12));
        var third = await BookAsync(mediumId, At(1, 10));

        var html = await _client.GetStringAsync($"/schedule?mediumId={mediumId}");

        Assert.Contains("<title>Schedule · Seance Scheduler</title>", html);
        var day0 = html.IndexOf($"id=\"{DayHeadingId(At(0, 0))}\"", StringComparison.Ordinal);
        var day1 = html.IndexOf($"id=\"{DayHeadingId(At(1, 0))}\"", StringComparison.Ordinal);
        Assert.True(day0 >= 0 && day1 > day0, "Both day headings are rendered in date order.");

        int Row(SeanceDto s) => html.IndexOf($"data-seance-id=\"{s.Id}\"", StringComparison.Ordinal);
        Assert.InRange(Row(first), day0, day1);
        Assert.InRange(Row(second), Row(first), day1);
        Assert.True(Row(third) > day1, "Next-day seance is under its own heading.");
    }

    [Fact]
    public async Task Get_FilterByMedium_ShowsOnlyThatMedium()
    {
        var mine = await CreateMediumAsync();
        var other = await CreateMediumAsync();
        var kept = await BookAsync(mine, At(2, 10));
        var hidden = await BookAsync(other, At(2, 10));

        var filtered = await _client.GetStringAsync($"/schedule?mediumId={mine}");
        Assert.Contains($"data-seance-id=\"{kept.Id}\"", filtered);
        Assert.DoesNotContain($"data-seance-id=\"{hidden.Id}\"", filtered);
        Assert.Contains($"<option value=\"{mine}\" selected", filtered);

        // "All mediums" submits an empty value: it must show everything, not fail binding.
        var all = await _client.GetAsync("/schedule?mediumId=");
        Assert.Equal(HttpStatusCode.OK, all.StatusCode);
        var allHtml = await all.Content.ReadAsStringAsync();
        Assert.Contains($"data-seance-id=\"{kept.Id}\"", allHtml);
        Assert.Contains($"data-seance-id=\"{hidden.Id}\"", allHtml);
    }

    [Fact]
    public async Task Cancel_Valid_RedirectsWithPrgKeepingFilter()
    {
        var mediumId = await CreateMediumAsync();
        var seance = await BookAsync(mediumId, At(3, 10));

        var response = await PostCancelAsync($"/schedule?mediumId={mediumId}", seance.Id.ToString());

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal($"/schedule?mediumId={mediumId}&cancelled=1", response.Headers.Location?.PathAndQuery);
        Assert.DoesNotContain(await ScheduledOfAsync(mediumId), s => s.Id == seance.Id);

        var html = await _client.GetStringAsync(response.Headers.Location!.PathAndQuery);
        Assert.Contains("role=\"status\"", html);
        Assert.DoesNotContain($"data-seance-id=\"{seance.Id}\"", html);
    }

    [Fact]
    public async Task Cancel_Twice_Returns409WithMessage()
    {
        var mediumId = await CreateMediumAsync();
        var seance = await BookAsync(mediumId, At(4, 10));
        await BookAsync(mediumId, At(4, 12)); // keeps the cancel form rendered after the first cancel
        Assert.Equal(HttpStatusCode.Found, (await PostCancelAsync("/schedule", seance.Id.ToString())).StatusCode);

        var response = await PostCancelAsync("/schedule", seance.Id.ToString());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("role=\"alert\"", html);
        Assert.Contains("already been cancelled", html);
    }

    [Fact]
    public async Task Cancel_Unknown_Returns400WithMessage()
    {
        var mediumId = await CreateMediumAsync();
        await BookAsync(mediumId, At(5, 10));

        var response = await PostCancelAsync("/schedule", "999999");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("role=\"alert\"", html);
        Assert.Contains("Seance 999999 not found.", html);
    }

    [Fact]
    public async Task Cancel_WithoutAntiforgeryToken_Returns400AndKeepsSeance()
    {
        var mediumId = await CreateMediumAsync();
        var seance = await BookAsync(mediumId, At(6, 10));

        var response = await PageForms.PostAsync(_client, "/schedule", "CancelSeance",
            [new("CancelId", seance.Id.ToString())], withToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains(await ScheduledOfAsync(mediumId), s => s.Id == seance.Id);
    }
}
