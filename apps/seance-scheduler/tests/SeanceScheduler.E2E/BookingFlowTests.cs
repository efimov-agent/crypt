using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Playwright;

namespace SeanceScheduler.E2E;

[Collection(E2ECollection.Name)]
public sealed class BookingFlowTests(E2EFixture fixture) : IAsyncLifetime
{
    // The app uses the real clock, so bookings go well into the future.
    private static readonly DateTime Day = DateTime.UtcNow.Date.AddDays(60);

    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _context = await fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public Task DisposeAsync() => _context.DisposeAsync().AsTask();

    private static string Local(int hour, int minute = 0) =>
        Day.AddHours(hour).AddMinutes(minute).ToString("yyyy-MM-ddTHH:mm", CultureInfo.InvariantCulture);

    private ILocator Row(string clientName) => _page.Locator("tr[data-seance-id]", new() { HasText = clientName });

    private async Task BookViaFormAsync(string mediumName, string clientName, string startsAt, string endsAt)
    {
        await _page.GotoAsync($"{fixture.BaseUrl}/schedule/new");
        await _page.GetByLabel("Spirit Medium").SelectOptionAsync(new SelectOptionValue { Label = mediumName });
        await _page.GetByLabel("Seeker's name").FillAsync(clientName);
        await _page.GetByLabel("Starts at (UTC)").FillAsync(startsAt);
        await _page.GetByLabel("Ends at (UTC)").FillAsync(endsAt);
        await _page.GetByRole(AriaRole.Button, new() { Name = "Summon This Seance" }).ClickAsync();
    }

    [Fact]
    public async Task Book_AppearsInSchedule()
    {
        var medium = await fixture.SeedMediumAsync();
        var client = $"Ichabod {Guid.NewGuid():N}"[..20];

        await BookViaFormAsync(medium.Name, client, Local(14), Local(14, 30));

        await Assertions.Expect(_page).ToHaveURLAsync(new Regex(@"/schedule\?booked=1$"));
        await Assertions.Expect(_page.GetByRole(AriaRole.Status)).ToContainTextAsync("booked");
        await Assertions.Expect(Row(client)).ToHaveCountAsync(1);
        await Assertions.Expect(Row(client)).ToContainTextAsync("14:00–14:30");
        await Assertions.Expect(Row(client)).ToContainTextAsync(medium.Name);
    }

    [Fact]
    public async Task Book_Overlapping_ShowsConflictAndKeepsValues()
    {
        var medium = await fixture.SeedMediumAsync();
        await BookViaFormAsync(medium.Name, "First seeker", Local(14), Local(14, 30));
        await Assertions.Expect(_page).ToHaveURLAsync(new Regex(@"/schedule\?booked=1$"));

        await BookViaFormAsync(medium.Name, "Second seeker", Local(14, 15), Local(14, 45));

        await Assertions.Expect(_page).ToHaveURLAsync(new Regex(@"/schedule/new$"));
        await Assertions.Expect(_page.GetByRole(AriaRole.Alert)).ToContainTextAsync("overlaps");
        await Assertions.Expect(_page.GetByLabel("Seeker's name")).ToHaveValueAsync("Second seeker");
        await Assertions.Expect(_page.GetByLabel("Starts at (UTC)")).ToHaveValueAsync(Local(14, 15));
        await Assertions.Expect(_page.GetByLabel("Ends at (UTC)")).ToHaveValueAsync(Local(14, 45));
        await Assertions.Expect(_page.GetByLabel("Spirit Medium").Locator("option:checked")).ToHaveTextAsync(medium.Name);
    }

    [Fact]
    public async Task Cancel_RemovesSeanceFromSchedule()
    {
        var medium = await fixture.SeedMediumAsync();
        var client = $"Katrina {Guid.NewGuid():N}"[..20];
        await BookViaFormAsync(medium.Name, client, Local(16), Local(17));
        await Assertions.Expect(Row(client)).ToHaveCountAsync(1);

        await Row(client).GetByRole(AriaRole.Button, new() { Name = $"Cancel seance of {client} at 16:00" }).ClickAsync();

        await Assertions.Expect(_page).ToHaveURLAsync(new Regex(@"/schedule\?cancelled=1$"));
        await Assertions.Expect(_page.GetByRole(AriaRole.Status)).ToContainTextAsync("cancelled");
        await Assertions.Expect(Row(client)).ToHaveCountAsync(0);
    }

    [Fact]
    public async Task UnknownUrl_ShowsNotFoundPage()
    {
        var response = await _page.GotoAsync($"{fixture.BaseUrl}/no-such-page");

        Assert.Equal(404, response!.Status);
        await Assertions.Expect(_page.GetByRole(AriaRole.Heading, new() { Level = 1 })).ToHaveTextAsync("Nothing haunts this address");
    }
}
