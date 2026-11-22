using Microsoft.Playwright;

namespace HauntedInventory.E2E;

[Collection(E2ECollection.Name)]
public sealed class ItemsPagesTests(E2EFixture fixture) : IAsyncLifetime
{
    private IBrowserContext _context = null!;
    private IPage _page = null!;

    public async Task InitializeAsync()
    {
        _context = await fixture.Browser.NewContextAsync();
        _page = await _context.NewPageAsync();
    }

    public Task DisposeAsync() => _context.DisposeAsync().AsTask();

    private ILocator Row(string name) => _page.Locator("table.items-table tbody tr", new() { HasText = name });

    [Fact]
    public async Task LowStockFilter_ShowsOnlyItemsAtOrBelowReorderLevel()
    {
        var plenty = await fixture.SeedItemAsync($"Coffin Nails {Guid.NewGuid():N}", quantity: 50, reorderLevel: 5);
        var atLevel = await fixture.SeedItemAsync($"Raven Feathers {Guid.NewGuid():N}", quantity: 3, reorderLevel: 3);

        await _page.GotoAsync($"{fixture.BaseUrl}/items");
        await Assertions.Expect(Row(plenty.Name)).ToHaveCountAsync(1);
        await Assertions.Expect(Row(atLevel.Name)).ToHaveCountAsync(1);
        await Assertions.Expect(Row(atLevel.Name).Locator(".badge-low")).ToHaveTextAsync("Low Stock");
        await Assertions.Expect(Row(plenty.Name).Locator(".badge-low")).ToHaveCountAsync(0);

        await _page.GetByLabel("Show only low stock items").CheckAsync();
        await _page.GetByRole(AriaRole.Button, new() { Name = "Filter" }).ClickAsync();

        await Assertions.Expect(_page).ToHaveURLAsync(new System.Text.RegularExpressions.Regex(@"/items\?lowStock=true$"));
        await Assertions.Expect(Row(atLevel.Name)).ToHaveCountAsync(1);
        await Assertions.Expect(Row(plenty.Name)).ToHaveCountAsync(0);
        await Assertions.Expect(_page.GetByLabel("Show only low stock items")).ToBeCheckedAsync();
    }

    [Fact]
    public async Task ViewLink_OpensItemDetails()
    {
        var item = await fixture.SeedItemAsync($"Candle Stubs {Guid.NewGuid():N}", quantity: 7, reorderLevel: 2);

        await _page.GotoAsync($"{fixture.BaseUrl}/items");
        await Row(item.Name).GetByRole(AriaRole.Link, new() { Name = "View" }).ClickAsync();

        await Assertions.Expect(_page).ToHaveURLAsync($"{fixture.BaseUrl}/items/{item.Id}");
        await Assertions.Expect(_page.Locator(".item-header h2")).ToHaveTextAsync(item.Name);
        await Assertions.Expect(_page.Locator(".quantity-display .value")).ToHaveTextAsync("7");
        await Assertions.Expect(_page.GetByText("No movements recorded yet.")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task RecordMovement_Success_RedirectsAndShowsUpdatedQuantityAndHistory()
    {
        var item = await fixture.SeedItemAsync($"Ectoplasm Jars {Guid.NewGuid():N}", quantity: 10, reorderLevel: 2);
        await _page.GotoAsync($"{fixture.BaseUrl}/items/{item.Id}");

        await _page.GetByLabel("Quantity Change").FillAsync("-4");
        await _page.GetByLabel("Reason").SelectOptionAsync("Sold");
        await _page.GetByLabel("Notes (optional)").FillAsync("sold to a medium");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Record Movement" }).ClickAsync();

        // Post/Redirect/Get: the browser ends on a GET URL, so a refresh cannot repeat the movement.
        await Assertions.Expect(_page).ToHaveURLAsync($"{fixture.BaseUrl}/items/{item.Id}?recorded=1");
        await Assertions.Expect(_page.Locator(".success-message")).ToContainTextAsync("Movement recorded successfully!");
        await Assertions.Expect(_page.Locator(".quantity-display .value")).ToHaveTextAsync("6");

        var rows = _page.Locator("table.movements-table tbody tr");
        await Assertions.Expect(rows).ToHaveCountAsync(1);
        var cells = rows.First.Locator("td");
        await Assertions.Expect(cells.Nth(1)).ToHaveTextAsync("Sold");
        await Assertions.Expect(cells.Nth(2)).ToHaveTextAsync("-4");
        await Assertions.Expect(cells.Nth(3)).ToHaveTextAsync("sold to a medium");
        await Assertions.Expect(cells.Nth(4)).ToHaveTextAsync("6");

        await _page.ReloadAsync();
        await Assertions.Expect(rows).ToHaveCountAsync(1);
        await Assertions.Expect(_page.Locator(".quantity-display .value")).ToHaveTextAsync("6");
    }

    [Fact]
    public async Task RecordMovement_InsufficientStock_Returns409AndKeepsQuantity()
    {
        var item = await fixture.SeedItemAsync($"Silver Bullets {Guid.NewGuid():N}", quantity: 3, reorderLevel: 1);
        await _page.GotoAsync($"{fixture.BaseUrl}/items/{item.Id}");

        await _page.GetByLabel("Quantity Change").FillAsync("-5");
        await _page.GetByLabel("Reason").SelectOptionAsync("Sold");
        var response = await _page.RunAndWaitForResponseAsync(
            () => _page.GetByRole(AriaRole.Button, new() { Name = "Record Movement" }).ClickAsync(),
            r => r.Request.Method == "POST" && r.Url.Contains($"/items/{item.Id}"));

        Assert.Equal(409, response.Status);
        await Assertions.Expect(_page.GetByRole(AriaRole.Alert)).ToContainTextAsync("Not enough stock");
        await Assertions.Expect(_page.Locator(".quantity-display .value")).ToHaveTextAsync("3");
        await Assertions.Expect(_page.Locator(".success-message")).ToHaveCountAsync(0);
        await Assertions.Expect(_page.GetByText("No movements recorded yet.")).ToBeVisibleAsync();
        // The submitted values are kept so the user can correct them.
        await Assertions.Expect(_page.GetByLabel("Quantity Change")).ToHaveValueAsync("-5");
        await Assertions.Expect(_page.GetByLabel("Reason")).ToHaveValueAsync("Sold");
    }

    [Fact]
    public async Task UnknownItem_ShowsNotFoundWith404()
    {
        var response = await _page.GotoAsync($"{fixture.BaseUrl}/items/999999");

        Assert.NotNull(response);
        Assert.Equal(404, response.Status);
        await Assertions.Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Nothing haunts this address" })).ToBeVisibleAsync();
    }

    [Fact]
    public async Task UnknownRoute_ShowsNotFoundPageWith404()
    {
        var response = await _page.GotoAsync($"{fixture.BaseUrl}/no-such-crypt");

        Assert.NotNull(response);
        Assert.Equal(404, response.Status);
        await Assertions.Expect(_page.GetByRole(AriaRole.Heading, new() { Name = "Nothing haunts this address" })).ToBeVisibleAsync();
        await Assertions.Expect(_page.GetByRole(AriaRole.Link, new() { Name = "Back to the inventory" })).ToBeVisibleAsync();
    }
}
