using System.Globalization;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Features.Mediums;
using SeanceScheduler.Web.Features.Seances;
using Xunit;

namespace SeanceScheduler.Tests.Features;

[Collection("Integration tests")]
public class ScheduleNewPageTests : IClassFixture<TestDatabaseFixture>
{
    private readonly HttpClient _client;

    // The app uses the real clock, so bookings go well into the future (other classes use other days).
    private static readonly DateTime Day = DateTime.UtcNow.Date.AddDays(40);

    public ScheduleNewPageTests(TestDatabaseFixture fixture)
    {
        _client = PageForms.CreateNoRedirectClient(fixture);
    }

    private static string Local(int hour, int minute = 0) =>
        Day.AddHours(hour).AddMinutes(minute).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    private async Task<int> CreateMediumAsync(bool active = true)
    {
        var response = await _client.PostAsJsonAsync("/api/mediums", new { name = $"Medium {Guid.NewGuid():N}" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var medium = (await response.Content.ReadFromJsonAsync<MediumDto>())!;
        if (!active)
        {
            (await _client.PostAsync($"/api/mediums/{medium.Id}/deactivate", null)).EnsureSuccessStatusCode();
        }

        return medium.Id;
    }

    private Task<HttpResponseMessage> PostBookAsync(string mediumId, string clientName, string startsAt, string endsAt) =>
        PageForms.PostAsync(_client, "/schedule/new", "BookSeance",
        [
            new("Form.MediumId", mediumId),
            new("Form.ClientName", clientName),
            new("Form.StartsAt", startsAt),
            new("Form.EndsAt", endsAt),
        ]);

    private async Task<List<SeanceDto>> SeancesOfAsync(int mediumId) =>
        (await _client.GetFromJsonAsync<List<SeanceDto>>($"/api/seances?mediumId={mediumId}"))!;

    [Fact]
    public async Task Book_Valid_RedirectsWithPrgAndStoresUtcTimes()
    {
        var mediumId = await CreateMediumAsync();
        var client = $"Seeker {Guid.NewGuid():N}";

        var response = await PostBookAsync(mediumId.ToString(CultureInfo.InvariantCulture), client, Local(10), Local(11));

        Assert.Equal(HttpStatusCode.Found, response.StatusCode);
        Assert.Equal("/schedule?booked=1", response.Headers.Location?.PathAndQuery);
        var seance = Assert.Single(await SeancesOfAsync(mediumId));
        Assert.Equal(client, seance.ClientName);
        Assert.Equal(Day.AddHours(10), seance.StartsAt.ToUniversalTime());
        Assert.Equal(Day.AddHours(11), seance.EndsAt.ToUniversalTime());

        var html = await _client.GetStringAsync("/schedule?booked=1");
        Assert.Contains("role=\"status\"", html);
        Assert.Contains(client, html);
    }

    [Fact]
    public async Task Book_Invalid_Returns400WithAriaAndKeepsValues()
    {
        var mediumId = await CreateMediumAsync();

        var response = await PostBookAsync(mediumId.ToString(CultureInfo.InvariantCulture), "", "not-a-date", Local(11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();

        var client = PageForms.ElementTag(html, "seance-client");
        Assert.NotNull(client);
        Assert.Contains("aria-invalid=\"true\"", client);
        Assert.Contains("aria-describedby=\"seance-clientname-error\"", client);
        Assert.NotNull(PageForms.ElementTag(html, "seance-clientname-error"));

        var start = PageForms.ElementTag(html, "seance-startsat");
        Assert.NotNull(start);
        Assert.Contains("aria-describedby=\"seance-startsat-error\"", start);
        Assert.Contains("value=\"not-a-date\"", start);
        Assert.NotNull(PageForms.ElementTag(html, "seance-startsat-error"));

        // A valid field keeps its value and is not marked invalid.
        var end = PageForms.ElementTag(html, "seance-endsat");
        Assert.NotNull(end);
        Assert.Contains($"value=\"{Local(11)}\"", end);
        Assert.DoesNotContain("aria-invalid", end);

        Assert.Empty(await SeancesOfAsync(mediumId));
    }

    [Fact]
    public async Task Book_NoMediumChosen_MarksSelectInvalid()
    {
        await CreateMediumAsync();

        var response = await PostBookAsync("", "Seeker", Local(10), Local(11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        var select = PageForms.ElementTag(html, "seance-medium");
        Assert.NotNull(select);
        Assert.Contains("aria-invalid=\"true\"", select);
        Assert.Contains("aria-describedby=\"seance-mediumid-error\"", select);
        Assert.NotNull(PageForms.ElementTag(html, "seance-mediumid-error"));
    }

    [Fact]
    public async Task Book_PastStart_Returns400FromSlotRules()
    {
        var mediumId = await CreateMediumAsync();
        var yesterday = DateTime.UtcNow.Date.AddDays(-1);
        string Past(int hour) => yesterday.AddHours(hour).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

        // Well-formed input that only the service rules (which know the clock) reject.
        var response = await PostBookAsync(mediumId.ToString(CultureInfo.InvariantCulture), "Seeker", Past(10), Past(11));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("Start time must be in the future.", html);
        Assert.Contains("aria-describedby=\"seance-startsat-error\"", PageForms.ElementTag(html, "seance-startsat"));
        Assert.Empty(await SeancesOfAsync(mediumId));
    }

    [Fact]
    public async Task Book_Overlap_Returns409WithMessageAndKeepsValues()
    {
        var mediumId = await CreateMediumAsync();
        var id = mediumId.ToString(CultureInfo.InvariantCulture);
        Assert.Equal(HttpStatusCode.Found, (await PostBookAsync(id, "First", Local(12), Local(13))).StatusCode);

        var response = await PostBookAsync(id, "Second", Local(12, 30), Local(13, 30));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.NotNull(PageForms.ElementTag(html, "seance-form-error"));
        Assert.Contains("overlaps", html);
        Assert.Contains("value=\"Second\"", PageForms.ElementTag(html, "seance-client"));
        Assert.Contains($"value=\"{Local(12, 30)}\"", PageForms.ElementTag(html, "seance-startsat"));
        Assert.Single(await SeancesOfAsync(mediumId));
    }

    [Fact]
    public async Task Book_UnknownMedium_Returns400WithMessage()
    {
        await CreateMediumAsync(); // the form only renders while some medium is active

        var response = await PostBookAsync("999999", "Lost soul", Local(14), Local(15));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.NotNull(PageForms.ElementTag(html, "seance-form-error"));
        Assert.Contains("Medium 999999 not found.", html);
    }

    [Fact]
    public async Task Book_InactiveMedium_Returns409()
    {
        await CreateMediumAsync();
        var inactive = await CreateMediumAsync(active: false);

        var response = await PostBookAsync(inactive.ToString(CultureInfo.InvariantCulture), "Seeker", Local(15), Local(16));

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var html = await response.Content.ReadAsStringAsync();
        Assert.Contains("inactive", html);
        // The departed choice stays visible (selected, not submittable) instead of silently resetting.
        Assert.Matches($"<option value=\"{inactive}\"[^>]*selected[^>]*disabled[^>]*>[^<]*\\(departed\\)", html);
        Assert.Empty(await SeancesOfAsync(inactive));
    }

    [Fact]
    public async Task Book_WithoutAntiforgeryToken_Returns400AndDoesNotBook()
    {
        var mediumId = await CreateMediumAsync();

        var response = await PageForms.PostAsync(_client, "/schedule/new", "BookSeance",
        [
            new("Form.MediumId", mediumId.ToString(CultureInfo.InvariantCulture)),
            new("Form.ClientName", "Forger"),
            new("Form.StartsAt", Local(17)),
            new("Form.EndsAt", Local(18)),
        ], withToken: false);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(await SeancesOfAsync(mediumId));
    }

    [Fact]
    public async Task Get_ListsOnlyActiveMediums()
    {
        var active = await CreateMediumAsync();
        var inactive = await CreateMediumAsync(active: false);

        var html = await _client.GetStringAsync("/schedule/new");

        Assert.Contains("<title>Book a Seance · Seance Scheduler</title>", html);
        Assert.Contains($"<option value=\"{active}\"", html);
        Assert.DoesNotContain($"<option value=\"{inactive}\"", html);
    }
}

// Separate class = separate fixture database with no mediums at all.
[Collection("Integration tests")]
public class ScheduleNewWithoutMediumsPageTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;

    public ScheduleNewWithoutMediumsPageTests(TestDatabaseFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task Get_ShowsEmptyStateWithoutForm()
    {
        var html = await (await PageForms.CreateNoRedirectClient(_fixture).GetAsync("/schedule/new")).Content.ReadAsStringAsync();

        Assert.Contains("No active medium", html);
        Assert.DoesNotContain("id=\"seance-medium\"", html);
    }

    [Fact]
    public async Task Post_AfterLastMediumDeparted_IsHandledByTheForm()
    {
        var client = PageForms.CreateNoRedirectClient(_fixture);
        var created = await client.PostAsJsonAsync("/api/mediums", new { name = $"Medium {Guid.NewGuid():N}" });
        var medium = (await created.Content.ReadFromJsonAsync<MediumDto>())!;

        // The seeker opened the form while the medium was active; it departs before they submit.
        var page = await client.GetAsync("/schedule/new");
        var token = Regex.Match(await page.Content.ReadAsStringAsync(), "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(token.Success);
        (await client.PostAsync($"/api/mediums/{medium.Id}/deactivate", null)).EnsureSuccessStatusCode();

        var response = await client.PostAsync("/schedule/new", new FormUrlEncodedContent(
        [
            new("_handler", "BookSeance"),
            new("__RequestVerificationToken", WebUtility.HtmlDecode(token.Groups[1].Value)),
            new("Form.MediumId", medium.Id.ToString(CultureInfo.InvariantCulture)),
            new("Form.ClientName", "Seeker"),
            new("Form.StartsAt", "2030-01-01T10:00"),
            new("Form.EndsAt", "2030-01-01T11:00"),
        ]));

        // Not the framework's "no form named BookSeance" 400: our handler answers with the domain conflict.
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.NotNull(PageForms.ElementTag(html, "seance-form-error"));
        Assert.Contains("No active medium", html);
        Assert.Contains("(departed)", html);
    }
}
