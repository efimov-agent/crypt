using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;
using SeanceScheduler.Tests.Fixtures;
using SeanceScheduler.Web.Features.Mediums;

namespace SeanceScheduler.E2E;

/// <summary>
/// Runs the real app on Kestrel (random port) against a throw-away SQLite file and drives it with headless Chromium.
/// Tests stay independent by creating their own uniquely named mediums instead of wiping the database.
/// </summary>
public sealed class E2EFixture : IAsyncLifetime
{
    private readonly TestDatabaseFixture _app = new();
    private IPlaywright? _playwright;

    public IBrowser Browser { get; private set; } = null!;
    public string BaseUrl { get; private set; } = string.Empty;

    public async Task InitializeAsync()
    {
        var exitCode = Microsoft.Playwright.Program.Main(["install", "chromium"]);
        if (exitCode != 0)
        {
            throw new InvalidOperationException($"Playwright browser install failed with exit code {exitCode}.");
        }

        _app.UseKestrel(0);
        _app.StartServer();
        BaseUrl = _app.ClientOptions.BaseAddress.ToString().TrimEnd('/');

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true });
    }

    public async Task<MediumDto> SeedMediumAsync()
    {
        using var scope = _app.Services.CreateScope();
        var mediums = scope.ServiceProvider.GetRequiredService<MediumsService>();
        return await mediums.CreateMediumAsync($"Madame {Guid.NewGuid():N}"[..20], null, CancellationToken.None);
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();
        await _app.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class E2ECollection : ICollectionFixture<E2EFixture>
{
    public const string Name = "e2e";
}
