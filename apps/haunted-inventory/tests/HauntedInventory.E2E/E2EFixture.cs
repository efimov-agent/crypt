using HauntedInventory.Tests.Fixtures;
using HauntedInventory.Web.Data;
using HauntedInventory.Web.Data.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Playwright;

namespace HauntedInventory.E2E;

/// <summary>
/// Runs the real app on Kestrel (random port) against a throw-away SQLite file and drives it with headless Chromium.
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

    public async Task<Item> SeedItemAsync(string name, int quantity, int reorderLevel)
    {
        using var scope = _app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var item = new Item
        {
            Name = name,
            Sku = $"E2E-{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            Quantity = quantity,
            ReorderLevel = reorderLevel,
        };
        db.Items.Add(item);
        await db.SaveChangesAsync();
        return item;
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
