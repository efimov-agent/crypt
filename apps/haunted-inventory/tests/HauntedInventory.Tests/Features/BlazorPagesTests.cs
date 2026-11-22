using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using HauntedInventory.Tests.Fixtures;
using HauntedInventory.Web.Data.Models;
using HauntedInventory.Web.Features.Items;
using Microsoft.AspNetCore.Mvc.Testing;
using Xunit;

namespace HauntedInventory.Tests.Features;

/// <summary>
/// Integration tests for Blazor static SSR pages (Tasks 10-12).
/// Tests GET /items, GET /items?lowStock=true, GET /items/{id}, GET /items/999999,
/// and POST movement form with antiforgery token.
/// </summary>
[Collection("Integration tests")]
public class BlazorPagesTests : IClassFixture<TestDatabaseFixture>
{
    private readonly TestDatabaseFixture _fixture;
    private readonly HttpClient _client;

    public BlazorPagesTests(TestDatabaseFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.CreateClient();
    }

    #region Task 10: Items List Page Tests

    [Fact]
    public async Task GetItemsPage_Returns200_AndContainsItemSku()
    {
        // Act
        var response = await _client.GetAsync("/items");

        // Assert: 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert: Contains item SKU (from seeded data)
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("TEST-001", content);
    }

    [Fact]
    public async Task GetItemsPage_LowStockFilter_ReturnsFilteredItems()
    {
        // Act: Get items with lowStock=true filter
        var response = await _client.GetAsync("/items?lowStock=true");

        // Assert: 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var content = await response.Content.ReadAsStringAsync();
        // Test Item 2 has Quantity=2, ReorderLevel=3 so it's low stock
        Assert.Contains("TEST-002", content);
        Assert.DoesNotContain("TEST-001", content); // 10 in stock, reorder level 5
    }

    #endregion

    #region Task 11: Item Details Page Tests

    [Fact]
    public async Task GetItemDetailsPage_Returns200_AndContainsItemSku()
    {
        // Arrange: Get an item ID from the seeded data
        var items = await _client.GetFromJsonAsync<List<ItemDto>>("/api/items");
        var item = items?.FirstOrDefault(i => i.Sku == "TEST-001");
        Assert.NotNull(item);

        // Act
        var response = await _client.GetAsync($"/items/{item.Id}");

        // Assert: 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert: Contains item SKU
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("TEST-001", content);
        Assert.Contains(item.Name, content);
    }

    [Fact]
    public async Task GetItemDetailsPage_NotFound_Returns404()
    {
        // Act
        var response = await _client.GetAsync("/items/999999");

        // Assert: 404 with the rendered not-found page (a bare 404 status used to leave the body empty)
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Nothing haunts this address", await response.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task UnknownRoute_Returns404WithNotFoundPage()
    {
        var response = await _client.GetAsync("/no-such-crypt");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Contains("Nothing haunts this address", await response.Content.ReadAsStringAsync());
    }

    #endregion

    #region Task 12: Movement Form POST with Antiforgery

    /// <summary>
    /// Posting the record-movement form with a valid antiforgery token records the movement
    /// and redirects back to the details page (Post/Redirect/Get).
    /// </summary>
    [Fact]
    public async Task PostMovementForm_WithValidData_RecordsMovementAndRedirects()
    {
        var item = await CreateItemWithStockAsync("MVMT-001", 10);
        using var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var postResponse = await PostMovementFormAsync(client, item.Id, delta: "-1", reason: "Sold", note: "Test sale");

        Assert.Equal(HttpStatusCode.Redirect, postResponse.StatusCode);
        Assert.NotNull(postResponse.Headers.Location);
        Assert.Equal($"/items/{item.Id}?recorded=1", postResponse.Headers.Location.PathAndQuery);

        var finalItem = await client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.NotNull(finalItem);
        Assert.Equal(9, finalItem.Quantity);

        // The page tracks the Item in the same DI scope; ResultingQuantity must still come from the database.
        var history = await client.GetFromJsonAsync<GetMovementsResponse>($"/api/items/{item.Id}/movements");
        Assert.NotNull(history);
        Assert.Equal(9, history.Movements[0].ResultingQuantity);
        Assert.Equal(-1, history.Movements[0].Delta);

        var page = await client.GetStringAsync($"/items/{item.Id}?recorded=1");
        Assert.Contains("Movement recorded successfully", page);
        Assert.Contains("Test sale", page);
    }

    /// <summary>
    /// Selling more than is in stock re-renders the page with a conflict message and 409, without recording anything.
    /// </summary>
    [Fact]
    public async Task PostMovementForm_InsufficientStock_Returns409WithMessage()
    {
        var item = await CreateItemWithStockAsync("INSTK-001", 2);
        using var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var postResponse = await PostMovementFormAsync(client, item.Id, delta: "-5", reason: "Sold", note: "Attempted sale");

        Assert.Equal(HttpStatusCode.Conflict, postResponse.StatusCode);
        Assert.Contains("Not enough stock", await postResponse.Content.ReadAsStringAsync());
        var finalItem = await client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.Equal(2, finalItem!.Quantity);
    }

    /// <summary>
    /// Invalid form input (zero delta) is rejected with 400 and a validation message.
    /// </summary>
    [Fact]
    public async Task PostMovementForm_ZeroDelta_Returns400WithMessage()
    {
        var item = await CreateItemWithStockAsync("ZERO-001", 3);
        using var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var postResponse = await PostMovementFormAsync(client, item.Id, delta: "0", reason: "Sold", note: "");

        Assert.Equal(HttpStatusCode.BadRequest, postResponse.StatusCode);
        Assert.Contains("non-zero", await postResponse.Content.ReadAsStringAsync(), StringComparison.OrdinalIgnoreCase);
        var finalItem = await client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.Equal(3, finalItem!.Quantity);
    }

    /// <summary>
    /// Without an antiforgery token the form post is refused and nothing is recorded.
    /// </summary>
    [Fact]
    public async Task PostMovementForm_WithoutAntiforgeryToken_IsRejected()
    {
        var item = await CreateItemWithStockAsync("NOAF-001", 3);
        using var client = _fixture.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });

        var response = await client.PostAsync($"/items/{item.Id}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "RecordMovement",
            ["Form.Delta"] = "-1",
            ["Form.Reason"] = "Sold",
        }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var finalItem = await client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.Equal(3, finalItem!.Quantity);
    }

    private async Task<ItemDto> CreateItemWithStockAsync(string sku, int quantity)
    {
        var createResponse = await _client.PostAsJsonAsync("/api/items", new { name = $"Form test {sku}", sku, reorderLevel = 0 });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);
        var stockResponse = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new { delta = quantity, reason = "Received" });
        Assert.Equal(HttpStatusCode.Created, stockResponse.StatusCode);
        return item;
    }

    private static async Task<HttpResponseMessage> PostMovementFormAsync(HttpClient client, int itemId, string delta, string reason, string note)
    {
        // GET first: sets the antiforgery cookie on this client and renders the matching form token.
        var html = await client.GetStringAsync($"/items/{itemId}");
        var token = ExtractAntiforgeryToken(html);
        Assert.False(string.IsNullOrEmpty(token), "Antiforgery token not found in the rendered form.");

        return await client.PostAsync($"/items/{itemId}", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["_handler"] = "RecordMovement",
            ["__RequestVerificationToken"] = token!,
            ["Form.Delta"] = delta,
            ["Form.Reason"] = reason,
            ["Form.Note"] = note,
        }));
    }

    /// <summary>
    /// Tests the full flow: Create item -> Add stock via API -> Record movement via form -> Verify.
    /// This test verifies the entire workflow end-to-end.
    /// </summary>
    [Fact]
    public async Task FullWorkflow_CreateItem_AddStock_RecordMovement_Verify()
    {
        // Step 1: Create an item
        var createResponse = await _client.PostAsJsonAsync("/api/items", new
        {
            name = "Full Flow Test",
            sku = "FULL-001",
            reorderLevel = 5
        });
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        var item = await createResponse.Content.ReadFromJsonAsync<ItemDto>();
        Assert.NotNull(item);
        
        // Step 2: Add stock via API
        var addStockResponse = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = 15,
            reason = "Received"
        });
        Assert.Equal(HttpStatusCode.Created, addStockResponse.StatusCode);
        
        // Step 3: Verify item has stock
        var itemWithStock = await _client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.NotNull(itemWithStock);
        Assert.Equal(15, itemWithStock.Quantity);
        
        // Step 4: View the items page
        var itemsPageResponse = await _client.GetAsync("/items");
        Assert.Equal(HttpStatusCode.OK, itemsPageResponse.StatusCode);
        var itemsPageContent = await itemsPageResponse.Content.ReadAsStringAsync();
        Assert.Contains("FULL-001", itemsPageContent);
        
        // Step 5: View item details
        var detailsResponse = await _client.GetAsync($"/items/{item.Id}");
        Assert.Equal(HttpStatusCode.OK, detailsResponse.StatusCode);
        var detailsContent = await detailsResponse.Content.ReadAsStringAsync();
        Assert.Contains("15", detailsContent); // quantity
        
        // Step 6: Record a movement via API
        var moveResponse = await _client.PostAsJsonAsync($"/api/items/{item.Id}/movements", new
        {
            delta = -3,
            reason = "Sold"
        });
        Assert.Equal(HttpStatusCode.Created, moveResponse.StatusCode);
        
        // Step 7: Verify movement history in API
        var movementsResponse = await _client.GetAsync($"/api/items/{item.Id}/movements");
        Assert.Equal(HttpStatusCode.OK, movementsResponse.StatusCode);
        var movementsResult = await movementsResponse.Content.ReadFromJsonAsync<GetMovementsResponse>();
        Assert.NotNull(movementsResult);
        Assert.Equal(2, movementsResult.Movements.Count); // 2 movements: +15 and -3
        
        // Step 8: Verify final quantity
        var finalItem = await _client.GetFromJsonAsync<ItemDto>($"/api/items/{item.Id}");
        Assert.NotNull(finalItem);
        Assert.Equal(12, finalItem.Quantity); // 15 - 3 = 12
    }

    #endregion

    #region Helper Methods

    private static string? ExtractAntiforgeryToken(string htmlContent)
    {
        // Look for the __RequestVerificationToken in a hidden input
        // Blazor generates: <input name="__RequestVerificationToken" type="hidden" value="..." />
        var tokenStart = htmlContent.IndexOf("name=\"__RequestVerificationToken\"", StringComparison.OrdinalIgnoreCase);
        if (tokenStart == -1)
            return null;

        var valueStart = htmlContent.IndexOf("value=\"", tokenStart, StringComparison.OrdinalIgnoreCase);
        if (valueStart == -1)
            return null;

        valueStart += 7; // Length of "value=\""
        var valueEnd = htmlContent.IndexOf("\"", valueStart);
        if (valueEnd == -1)
            return null;

        return htmlContent.Substring(valueStart, valueEnd - valueStart);
    }

    #endregion
}